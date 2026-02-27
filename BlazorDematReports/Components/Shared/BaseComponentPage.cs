using BlazorDematReports.Components.Dialog;
using BlazorDematReports.Components.Layout;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Services;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;



namespace BlazorDematReports.Components.Shared
{
    /// <summary>
    /// Base component page that provides common functionality for pages in the application.
    /// </summary>
    /// <typeparam name="TLogger"></typeparam>
    public partial class BaseComponentPage<TLogger> : ComponentBase, IDisposable
    {

        [Inject] protected UiStateService? UiState { get; set; }
        [Inject] protected NotificationDialog? NotificationDialog { get; set; }
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
            NotificationDialog notificationDialog,
            ILogger<TLogger> logger,
            ConfigUser configUser,
            IJSRuntime jsinterop,
            IDialogService dialogService,
            ILavorazioniConfigManager? lavorazioniConfigManager)
        {
            NotificationDialog = notificationDialog;
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

        public void Dispose()
        {
            if (EditContext is not null)
            {
                EditContext.OnFieldChanged -= EditContext_HandleFieldChanged!;
            }

            if (uEditContext is not null)
            {
                uEditContext.OnFieldChanged -= UEditContext_HandleFieldChanged!;
            }
        }
    }
}

