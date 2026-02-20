using AutoMapper;
using BlazorDematReports.Components.Dialog;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace BlazorDematReports.Components.Shared
{
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
            Model = new TModel();
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

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
                Model = new TModel();

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
                Logger?.LogError(ex, "{Message}", ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");
            }
        }

        protected void CanceledEditingItem(TValueBeforeEdit item)
        {
            if (ValueBeforeEdit != null)
                item = Mapper!.Map<TValueBeforeEdit>(ValueBeforeEdit);
        }

        protected ValidationMessageStore? ValidationMessages { get; private set; }

        protected virtual void ResetFormModel()
        {
            Model = new TModel();
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += EditContext_HandleFieldChanged!;

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
        protected virtual async Task StartedEditingItem(TValueBeforeEdit item)
        {
            try
            {
                ValueBeforeEdit = Mapper!.Map<TValueBeforeEdit>(item);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "{Message}", ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");
            }
        }

        /// <summary>
        /// Method called when editing of an item is canceled.
        /// </summary>
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
        protected virtual async Task StartedEditingItem1(UValueBeforeEdit item)
        {
            try
            {
                uValueBeforeEdit = Mapper!.Map<UValueBeforeEdit>(item);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "{Message}", ex.Message);
                if (NotificationDialog != null)
                    await NotificationDialog.ShowNotification("Error on StartedEditingItem", "OK", Color.Error, "Error");
            }
        }

        /// <summary>
        /// Method called when editing of an item is canceled.
        /// </summary>
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
