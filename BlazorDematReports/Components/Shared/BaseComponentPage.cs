using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Components.Dialog;
using BlazorDematReports.Components.Layout;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using BlazorDematReports.Services.UIServices;
using Entities.Helpers;
using Entities.Models.DbApplication;
using BlazorDematReports.Core.Utility.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using MudBlazor;


namespace BlazorDematReports.Components.Shared
{
    /// <summary>
    /// Base component page that provides common functionality for pages in the application.
    /// </summary>
    /// <typeparam name="TLogger"></typeparam>
    public class BaseComponentPage<TLogger> : ComponentBase, IDisposable
    {

        [Inject] protected UiStateService? UiState { get; set; }
        [Inject] protected IMapper? Mapper { get; set; }
        [Inject] protected NotificationDialog? NotificationDialog { get; set; }
        [Inject] protected IServiceWrapper? ServiceWrapper { get; set; }
        [Inject] protected ILogger<TLogger>? Logger { get; set; }
        [Inject] protected ConfigUser? ConfigUser { get; set; }
        [Inject] protected IJSRuntime? JSInterop { get; set; }
        [Inject] protected ISnackbar? Snackbar { get; set; }
        [Inject] protected IDialogService? DialogService { get; set; }
        [Inject] protected ILavorazioniConfigManager? LavorazioniConfigManager { get; set; }

        [CascadingParameter] public MainLayout? Layout { get; set; }


        protected DefaultFocus DefaultFocus { get; set; } = DefaultFocus.None;
        protected DataGridFilterMode FilterMode { get; set; } = DataGridFilterMode.ColumnFilterMenu;
        protected DataGridFilterCaseSensitivity CaseSensitivity { get; set; } = DataGridFilterCaseSensitivity.CaseInsensitive;



        /// <summary>gets/sets the visibility of the button Save in a Form </summary>
        protected bool IsFormValid { get; set; } = false;
        protected string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Context for the principal Form
        /// </summary>
        protected EditContext? EditContext;

        /// <summary>
        /// Context for the secondary Form
        /// </summary>
        protected EditContext? uEditContext;

        public BaseComponentPage() { }


        public BaseComponentPage(
            IMapper mapper,
            NotificationDialog notificationDialog,
            IServiceWrapper serviceWrapper,
            ILogger<TLogger> logger,
            ConfigUser configUser,
            IJSRuntime jsinterop,
            IDialogService dialogService,
            ILavorazioniConfigManager? lavorazioniConfigManager)
        {
            Mapper = mapper;
            NotificationDialog = notificationDialog;
            ServiceWrapper = serviceWrapper;
            Logger = logger;
            ConfigUser = configUser;
            JSInterop = jsinterop;
            DialogService = dialogService;
            LavorazioniConfigManager = lavorazioniConfigManager;
        }



        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }

        /// <summary>
        /// Logs query execution with structured information for better traceability.
        /// Uses the shared QueryLoggingHelper for consistency across the application.
        /// </summary>
        /// <param name="queryDescription">Description of the SQL query being executed</param>
        protected void LogQueryExecution(string? queryDescription = null)
        {
            if (Logger != null)
            {
                QueryLoggingHelper.LogQueryExecution(logger: Logger, additionalInfo: queryDescription);
            }
        }


        /// <summary>
        /// Run an action with an overlay message, handling exceptions and showing appropriate notifications.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="overlayText"></param>
        /// <returns></returns>
        protected async Task RunWithOverlay(Func<Task> action, string overlayText = "")
        {
            try
            {
                UiState?.SetMenuDisabled(true);
                UiState?.ShowOverlay(overlayText);
                ErrorMessage = string.Empty;
                Layout!.ResetFooter();

                await action();
            }
            catch (SqlException ex)
            {
                Logger?.LogError(ex, "Errore SQL: {Message}", ex.Message);
                ErrorMessage = "Errore di connessione al database.";
                await NotificationDialog!.ShowNotification("Errore di connessione al database.", "OK", Color.Error, "Error");
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 2627)
            {
                Logger?.LogError(ex, "Violazione vincolo univocità: {Message}", ex.Message);
                ErrorMessage = "Attenzione! Record duplicato.";
                await NotificationDialog!.ShowNotification("Attenzione! Record duplicato.", "OK", Color.Error, "Error");
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                Logger?.LogError(ex, "Violazione vincolo di integrità: {Message}", ex.Message);
                ErrorMessage = "Errore! Record mancanti o vincoli non rispettati.";
                await NotificationDialog!.ShowNotification("Errore! Record mancanti o vincoli non rispettati.", "OK", Color.Error, "Error");
            }
            catch (DbUpdateException ex)
            {
                Logger?.LogError(ex, "Errore di aggiornamento DB: {Message}", ex.Message);
                ErrorMessage = "Errore di aggiornamento dati.";
                await NotificationDialog!.ShowNotification("Errore di aggiornamento dati.", "OK", Color.Error, "Error");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, ex.Message);
                ErrorMessage = ex.Message;
                await NotificationDialog!.ShowNotification("Errore", "OK", Color.Error, "Error");
            }
            finally
            {
                UiState?.HideOverlay();
                UiState?.SetMenuDisabled(false);
            }
        }


        /// <summary>
        /// Handle field changes in the EditContext.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EditContext_HandleFieldChanged(object? sender, FieldChangedEventArgs e)
        {
            IsFormValid = EditContext?.Validate() ?? false;
            InvokeAsync(StateHasChanged);
        }


        /// <summary>
        /// Handle field changes in the uEditContext.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void UEditContext_HandleFieldChanged(object sender, FieldChangedEventArgs e)
        {
            IsFormValid = uEditContext?.Validate() ?? false;
            InvokeAsync(StateHasChanged);
        }


        #region Region Property Dto


        /// <summary>List Objects ProduzioneSistemaDto used by Form data</summary>
        protected List<ProduzioneSistemaDto>? ListProduzioneSistemaDto { get; set; }

        /// <summary>List Objects ProcedureLavorazioniDto</summary>
        protected List<ProcedureLavorazioniDto>? ListProcedureLavorazioniDto { get; set; }

        /// <summary>List Objects OperatoriDto</summary>
        protected List<OperatoriDto>? ListOperatoriDto { get; set; }

        /// <summary>List Objects ListClientiDto</summary>
        protected List<ClientiDto>? ListClientiDto { get; set; }

        /// <summary>List Objects LstFasiLavorazioneDto</summary>
        protected List<FasiLavorazioneDto>? ListFasiLavorazioneDto { get; set; }

        /// <summary>List Objects ListProcedureClienteDto</summary>
        protected List<ProcedureClienteDto>? ListProcedureClienteDto { get; set; }

        /// <summary>List Objects ListTipologieTotaliDto</summary>
        protected List<TipologieTotaliDto>? ListTipologieTotaliDto { get; set; }

        /// <summary>List Objects ListOperatoriNormalizzatiDto</summary>
        protected List<OperatoriNormalizzatiDto>? ListOperatoriNormalizzatiDto { get; set; }

        /// <summary>List Objects ListLavorazioniFasiTipoTotaleDto</summary>
        protected List<LavorazioniFasiTipoTotaleDto>? ListLavorazioniFasiTipoTotaleDto { get; set; }

        /// <summary>List Objects ListProduzioneOperatoriDto</summary>
        protected List<ProduzioneOperatoriDto>? ListProduzioneOperatoriDto { get; set; }

        /// <summary>List Objects ListTaskDataReadingAggiornamentoDto</summary>
        protected List<TaskDataReadingAggiornamentoDto>? ListTaskDataReadingAggiornamentoDto { get; set; }

        /// <summary>List Objects RuoliDto</summary>
        protected List<RuoliDto>? ListRuoliDto { get; set; }
        #endregion


        #region Region Property Objects DB

        /// <summary>List Objects Ruoli used by Form data </summary>
        protected List<Ruoli>? ListRuoli { get; set; }

        /// <summary>List Objects Operatori used by Form data </summary>
        protected List<Operatori>? ListOperatori { get; set; }

        /// <summary> List Objects Procedure Lavorazioni used by Form data</summary>
        protected List<ProcedureLavorazioni>? ListProcedureLavorazioni { get; set; }

        /// <summary> List Objects Fasi Lavorazioni used by Form data</summary>
        public List<FasiLavorazione>? ListFasiLavorazione { get; set; }

        /// <summary> List Objects Centri used by Form data</summary>
        protected List<CentriLavorazione>? ListCentri { get; set; }

        /// <summary> List Objects Tipologie Totali used by Form data</summary>
        protected List<TipologieTotali>? ListTipologieTotali { get; set; }

        /// <summary> List Objects Clienti used by Form data</summary>
        protected List<Clienti>? ListClienti { get; set; }

        /// <summary> List Objects ProcedureCliente used by Form data</summary>
        protected List<ProcedureCliente>? ListProcedureCliente { get; set; }

        /// <summary> List Objects Reparti used by Form data</summary>
        protected List<RepartiProduzione>? ListReparti { get; set; }

        /// <summary> List Objects FormatoDati used by Form data</summary>
        protected List<FormatoDati>? ListFormatoDati { get; set; }

        /// <summary> List Objects Turni used by Form data</summary>
        protected List<Turni>? ListTurni { get; set; }

        /// <summary> List Objects TipoTurni used by Form data</summary>
        protected List<TipoTurni>? ListTipoTurni { get; set; }

        protected List<TaskDataReadingAggiornamento>? ListTaskDataReadingAggiornamento { get; set; }

        /// <summary> List Objects Task used by Form data</summary>
        public List<TabellaTask>? ListTask { get; set; }



        #endregion


        #region Region Select Search

        protected List<string> SelectOperatore { get; private set; } = new();
        protected List<string> SelectLavorazione { get; private set; } = new();
        public List<string> SelectFase { get; private set; } = new();
        protected List<string> SelectTipologiaTotale { get; private set; } = new();
        protected List<string> SelectCliente { get; private set; } = new();
        protected List<string> SelectProceduraCliente { get; set; } = new();
        protected List<string> SelectReparto { get; set; } = new();
        protected List<string> SelectFormato { get; set; } = new();
        protected List<string> SelectTurno { get; set; } = new();
        protected List<string> SelectTipoTurno { get; set; } = new();
        protected List<string> SelectRuolo { get; set; } = new();

        /// <summary>
        /// Metodo generico per popolare una select
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="orderSelector"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        protected List<string> SetSelectList<T, TKey>(
            IEnumerable<T>? source,
            Func<T, TKey> orderSelector,
            Func<T, string?> valueSelector)
        {
            return source == null
                ? new List<string>()
                : source.OrderBy(orderSelector)
                        .Select(valueSelector)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()!;
        }

        /// <summary>
        /// Popola la select degli operatori (aggiunge la sigla se admin)
        /// </summary>
        protected virtual void SetSelectOperatore()
        {
            if (ConfigUser!.IsAdminRole)
                SelectOperatore = SetSelectList(
                    ListOperatori,
                    x => x.Operatore,
                    x => $"{x.Operatore}({x.IdcentroNavigation.Sigla})"
                );
            else
                SelectOperatore = SetSelectList(
                    ListOperatori,
                    x => x.Operatore,
                    x => x.Operatore
                );
        }

        /// <summary>
        /// Popola la select delle lavorazioni (tutte)
        /// </summary>
        protected virtual void SetSelectLavorazione()
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioni,
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni da una lista di DTO
        /// </summary>
        /// <param name="listProcedureLavorazioniDto"></param>
        protected virtual void SetSelectLavorazione(List<ProcedureLavorazioniDto> listProcedureLavorazioniDto)
        {
            SelectLavorazione = SetSelectList(
                listProcedureLavorazioniDto,
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni DTO filtrando per centro
        /// </summary>
        /// <param name="idCentro"></param>
        protected virtual void SetSelectLavorazioneDto(int idCentro)
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioniDto?.Where(x => x.Idcentro == idCentro),
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni filtrando per centro
        /// </summary>
        /// <param name="idCentro"></param>
        protected virtual void SetSelectLavorazione(int idCentro)
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioni?.Where(x => x.Idcentro == idCentro),
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle fasi di lavorazione (tutte)
        /// </summary>
        protected virtual void SetSelectFasi()
        {
            SelectFase = SetSelectList(
                ListFasiLavorazione,
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select delle fasi di una specifica lavorazione DTO
        /// </summary>
        /// <param name="procedureLavorazioniDto"></param>
        protected virtual void SetSelectFasi(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            SelectFase = SetSelectList(
                procedureLavorazioniDto?.LavorazioniFasiDataReadingsDto,
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select delle fasi con flag DataReading = true
        /// </summary>
        /// <param name="procedureLavorazioniDto"></param>
        protected virtual void SetSelectFasiOnlyWithDataReading(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            SelectFase = SetSelectList(
                procedureLavorazioniDto?.LavorazioniFasiDataReadingsDto?.Where(x => x.FlagDataReading),
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select dei clienti
        /// </summary>
        protected void SetSelectCliente()
        {
            SelectCliente = SetSelectList(
                ListClienti,
                x => x.NomeCliente,
                x => x.NomeCliente
            );
        }

        /// <summary>
        /// Popola la select dei reparti
        /// </summary>
        protected virtual void SetSelectReparto()
        {
            SelectReparto = SetSelectList(
                ListReparti,
                x => x.Reparti,
                x => x.Reparti
            );
        }

        /// <summary>
        /// Popola la select dei formati dati
        /// </summary>
        protected virtual void SetSelectFormato()
        {
            SelectFormato = SetSelectList(
                ListFormatoDati,
                x => x.FormatoDatiProduzione,
                x => x.FormatoDatiProduzione
            );
        }

        /// <summary>
        /// Popola la select delle procedure cliente
        /// </summary>
        protected virtual void SetSelectProcedureClienti()
        {
            SelectProceduraCliente = SetSelectList(
                ListProcedureCliente,
                x => x.ProceduraCliente,
                x => x.ProceduraCliente
            );
        }

        /// <summary>
        /// Popola la select delle tipologie totali
        /// </summary>
        protected virtual void SetSelectTipologieTotali()
        {
            SelectTipologiaTotale = SetSelectList(
                ListTipologieTotali,
                x => x.TipoTotale,
                x => x.TipoTotale
            );
        }

        /// <summary>
        /// Popola la select dei turni
        /// </summary>
        protected virtual void SetSelectTurni()
        {
            SelectTurno = SetSelectList(
                ListTurni,
                x => x.Turno,
                x => x.Turno
            );
        }

        /// <summary>
        /// Popola la select dei tipi turno
        /// </summary>
        protected virtual void SetSelectTipoTurni()
        {
            SelectTipoTurno = SetSelectList(
                ListTipoTurni,
                x => x.TipoTurno,
                x => x.TipoTurno
            );
        }

        /// <summary>
        /// Popola la select dei ruoli
        /// </summary>
        protected virtual void SetSelectRuolo()
        {
            SelectRuolo = SetSelectList(
                ListRuoli,
                x => x.Ruolo,
                x => x.Ruolo
            );
        }



        /// <summary>
        /// Used for select list into datagrid - Return string from list of object
        /// </summary>
        /// <param name="Select"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected async Task<IEnumerable<string>> SearchFromSelect(List<string>? Select, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return await Task.FromResult(Select!.AsEnumerable());

            return await Task.FromResult(Select!.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList().AsEnumerable());
        }



        #endregion


        /// <summary>
        /// Get Operatore Object tramite parametro stringa  => nome operatore 
        /// Se ruolo è Admin toglie la sigla del centro da elenco di nomi
        /// </summary>
        /// <param name="operatore"></param>
        /// <returns></returns>
        protected Operatori? GetOperatore(string operatore)
        {
            if (string.IsNullOrWhiteSpace(operatore) || ListOperatori == null)
                return null;

            string[] strArray = operatore.Split('(');
            string operatoreString = strArray[0];
            return ListOperatori.FirstOrDefault(x => x.Operatore == operatoreString);
        }




        /// <summary>
        /// Handles exceptions by logging them and showing a notification dialog with an error message.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="customMessage"></param>
        /// <returns></returns>
        protected async Task HandleExceptionFormAsync(Exception ex, string? customMessage = null)
        {
            UiState?.HideOverlay();
            UiState?.SetMenuDisabled(false);

            string message = customMessage;

            switch (ex)
            {
                case DbUpdateException dbEx when dbEx.InnerException is SqlException sqlEx:
                    message = sqlEx.Number switch
                    {
                        2627 => "Record duplicato.",
                        547 => "Vincolo di integrità violato.",
                        _ => "Errore di aggiornamento dati."
                    };
                    break;

                case SqlException sqlEx:
                    message = sqlEx.Number switch
                    {
                        2627 => "Record duplicato.",
                        547 => "Vincolo di integrità violato.",
                        _ => $"Errore SQL: {sqlEx.Message}"
                    };
                    break;

                case AutoMapperConfigurationException autoEx:
                    message = $"Errore di configurazione AutoMapper: {autoEx.Message}";
                    break;

                case NullReferenceException nullRefEx:
                case InvalidOperationException invalidOpEx:
                case ArgumentNullException argNullEx:
                    message = $"Errore applicativo: {ex.Message}";
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(message))
                        message = $"Errore generico: {ex.Message}";
                    break;
            }

            ErrorMessage = message;
            Logger?.LogError(ex, message);

            if (NotificationDialog != null)
                await NotificationDialog.ShowNotification(message, "OK", Color.Error, "Error");
        }


        public void Dispose()
        {
            if (EditContext is not null)
            {
                EditContext.OnFieldChanged -= EditContext_HandleFieldChanged!;
            }
        }
    }



    /// <summary>
    /// Base component page that provides common functionality for pages in the application with a model.
    /// </summary>
    /// <typeparam name="TLogger"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public class BaseComponentPage<TLogger, TModel> : BaseComponentPage<TLogger> where TModel : class, new()
    {
        public BaseComponentPage() { }

        public BaseComponentPage(
            IMapper mapper,
            NotificationDialog notificationDialog,
            IServiceWrapper serviceWrapper,
            ILogger<TLogger> logger,
            ConfigUser configUser,
            IJSRuntime jsinterop,
            IDialogService dialogService,
            ILavorazioniConfigManager lavorazioniConfigManager)
            : base(mapper, notificationDialog, serviceWrapper, logger, configUser, jsinterop, dialogService, lavorazioniConfigManager)
        { }

        /// <summary>
        /// Model used by the form
        /// </summary>
        public TModel? Model { get; set; }

        /// <summary>
        /// Sets the EditContext for the form.
        /// </summary>
        public void SetEditContext()
        {
            Model = new TModel();
            EditContext = new(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

            EditContext.OnValidationStateChanged += (sender, args) =>
            {
                IsFormValid = !EditContext.GetValidationMessages().Any();
                StateHasChanged();
            };
        }



        protected ValidationMessageStore? ValidationMessages { get; private set; }

        protected virtual void ResetFormModel()
        {
            // Ricrea il modello e l'EditContext
            Model = new TModel();
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

            // Svuota eventuali errori di validazione custom
            ValidationMessages = new ValidationMessageStore(EditContext);
            ValidationMessages.Clear();
            EditContext.NotifyValidationStateChanged();

            IsFormValid = false;
            ErrorMessage = string.Empty;
        }

    }



    /// <summary>
    /// Base component page that provides common functionality for pages in the application with a model and a value before edit.
    /// </summary>
    /// <typeparam name="TLogger"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TValueBeforeEdit"></typeparam>
    public class BaseComponentPage<TLogger, TModel, TValueBeforeEdit> : BaseComponentPage<TLogger> where TModel : class, new()
    {
        public BaseComponentPage() { }

        public BaseComponentPage(
            IMapper mapper,
            NotificationDialog notificationDialog,
            IServiceWrapper serviceWrapper,
            ILogger<TLogger> logger,
            ConfigUser configUser,
            IJSRuntime jsinterop,
            IDialogService dialogService,
            ILavorazioniConfigManager lavorazioniConfigManager)
            : base(mapper, notificationDialog, serviceWrapper, logger, configUser, jsinterop, dialogService, lavorazioniConfigManager)
        { }


        /// <summary>
        /// Model used by the form
        /// </summary>
        protected TModel? Model { get; set; }

        /// <summary>
        /// Model Row used by Edit in datagrid
        /// </summary>
        protected TValueBeforeEdit? ValueBeforeEdit { get; set; }


        /// <summary>
        /// Sets the EditContext for the form.
        /// </summary>
        protected void SetEditContext()
        {
            if (Model == null)
                Model = new TModel(); // Solo se non già valorizzato

            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;
            EditContext.OnValidationStateChanged += (sender, args) =>
            {
                IsFormValid = !EditContext.GetValidationMessages().Any();
                StateHasChanged();
            };
        }



        protected virtual async Task StartedEditingItem(TValueBeforeEdit item)
        {
            try
            {
                ValueBeforeEdit = Mapper!.Map<TValueBeforeEdit>(item);

            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");
            }
        }


        protected void CanceledEditingItem(TValueBeforeEdit item)
        {
            if (ValueBeforeEdit != null)
                item = Mapper!.Map<TValueBeforeEdit>(ValueBeforeEdit);
        }


        //protected void ResetModel()
        //{

        //    if (Model == null) return;
        //    foreach (var p in typeof(TModel).GetProperties())
        //        p.SetValue(Model, null);

        //}

        protected ValidationMessageStore? ValidationMessages { get; private set; }

        protected virtual void ResetFormModel()
        {
            // Ricrea il modello e l'EditContext
            Model = new TModel();
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

            // Svuota eventuali errori di validazione custom
            ValidationMessages = new ValidationMessageStore(EditContext);
            ValidationMessages.Clear();
            EditContext.NotifyValidationStateChanged();

            IsFormValid = false;
            ErrorMessage = string.Empty;
        }
    }



    /// <summary>
    /// Base component page that provides common functionality for pages in the application with two models and their values before edit.
    /// </summary>
    /// <typeparam name="TLogger"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TValueBeforeEdit"></typeparam>
    /// <typeparam name="UModel"></typeparam>
    /// <typeparam name="UValueBeforeEdit"></typeparam>
    public class BaseComponentPage<TLogger, TModel, TValueBeforeEdit, UModel, UValueBeforeEdit> : BaseComponentPage<TLogger> where TModel : class, new() where UModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseComponentPage{TLogger, TModel, TValueBeforeEdit, UModel, UValueBeforeEdit}"/> class.
        /// </summary>
        public BaseComponentPage() { }

        public BaseComponentPage(
            IMapper mapper,
            NotificationDialog notificationDialog,
            IServiceWrapper serviceWrapper,
            ILogger<TLogger> logger,
            ConfigUser configUser,
            IJSRuntime jsinterop,
            IDialogService dialogService,
            ILavorazioniConfigManager lavorazioniConfigManager)
            : base(mapper, notificationDialog, serviceWrapper, logger, configUser, jsinterop, dialogService, lavorazioniConfigManager)
        { }


        /// <summary>
        /// Model used by the form
        /// </summary>
        protected TModel? Model { get; set; }

        /// <summary>
        /// Model Row used by Edit in datagrid
        /// </summary>
        protected TValueBeforeEdit? ValueBeforeEdit { get; set; }

        /// <summary>
        /// Model used by the form
        /// </summary>
        protected void SetEditContext()
        {
            Model = new TModel();
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

            EditContext.OnValidationStateChanged += (sender, args) =>
            {
                IsFormValid = !EditContext.GetValidationMessages().Any();
                StateHasChanged();
            };
        }


        /// <summary>
        /// Method called when an item is started to be edited.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual async Task StartedEditingItem(TValueBeforeEdit item)
        {
            try
            {
                ValueBeforeEdit = Mapper!.Map<TValueBeforeEdit>(item);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");

            }
        }


        /// <summary>
        /// Method called when editing of an item is canceled.
        /// </summary>
        /// <param name="item"></param>
        protected void CanceledEditingItem(TValueBeforeEdit item)
        {
            if (ValueBeforeEdit != null)
                item = Mapper!.Map<TValueBeforeEdit>(ValueBeforeEdit);
        }


        /// <summary>
        /// alternative Model used by the form
        /// </summary>
        protected UModel? uModel { get; set; }

        /// <summary>
        /// alternative Model Row used by Edit in datagrid
        /// </summary>
        protected UValueBeforeEdit? uValueBeforeEdit { get; set; }


        /// <summary>
        /// Sets the EditContext for the alternative form.
        /// </summary>
        protected void USetEditContext()
        {
            uModel = new UModel();
            uEditContext = new EditContext(uModel);
            uEditContext.OnFieldChanged += UEditContext_HandleFieldChanged!;

            uEditContext.OnValidationStateChanged += (sender, args) =>
            {
                IsFormValid = !uEditContext.GetValidationMessages().Any();
                StateHasChanged();
            };
        }

        /// <summary>
        /// Method called when an item is started to be edited for the second model.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual async Task StartedEditingItem1(UValueBeforeEdit item)
        {
            try
            {
                uValueBeforeEdit = Mapper!.Map<UValueBeforeEdit>(item);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");
            }
        }

        /// <summary>
        /// Method called when editing of an item is canceled.
        /// </summary>
        /// <param name="item"></param>
        protected void CanceledEditingItem1(UValueBeforeEdit item)
        {
            if (uValueBeforeEdit != null)
                item = Mapper!.Map<UValueBeforeEdit>(uValueBeforeEdit);
        }


        /// <summary>
        /// Reset all properties of the second model
        /// </summary>
        protected void ResetUModel()
        {

            if (uModel == null)
                return;
            foreach (var p in typeof(UModel).GetProperties())
                p.SetValue(uModel, null);
        }

    }
}

