using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace BlazorDematReports.Components.Shared
{
    /// <summary>
    /// Gestione errori e overlay per BaseComponentPage.
    /// </summary>
    public partial class BaseComponentPage<TLogger>
    {
        /// <summary>
        /// Esegue un'azione con overlay UI, delegando la gestione degli errori a <see cref="HandleExceptionFormAsync"/>.
        /// </summary>
        /// <param name="action">Azione asincrona da eseguire.</param>
        /// <param name="overlayText">Testo mostrato nell'overlay durante l'esecuzione.</param>
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
            catch (OperationCanceledException)
            {
                // Componente dismesso durante il caricamento (navigazione): abort silenzioso
                Logger?.LogDebug("Operazione annullata per navigazione del componente");
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource già dismesso (race condition con DisposeAsync): abort silenzioso
                Logger?.LogDebug("Operazione interrotta: componente dismesso durante il caricamento");
            }
            catch (Exception ex)
            {
                await HandleExceptionFormAsync(ex);
            }
            finally
            {
                UiState?.HideOverlay();
                UiState?.SetMenuDisabled(false);
            }
        }

        /// <summary>
        /// Gestisce le eccezioni: logga l'errore, imposta <see cref="ErrorMessage"/> e mostra una notifica all'utente.
        /// Nasconde l'overlay e riabilita il menu prima di notificare.
        /// Usato direttamente dai gestori di form e delegato da <see cref="RunWithOverlay"/>.
        /// </summary>
        /// <param name="ex">Eccezione da gestire.</param>
        /// <param name="customMessage">Messaggio personalizzato opzionale che sovrascrive il messaggio derivato dall'eccezione.</param>
        protected async Task HandleExceptionFormAsync(Exception ex, string? customMessage = null)
        {
            UiState?.HideOverlay();
            UiState?.SetMenuDisabled(false);

            string message = customMessage ?? string.Empty;

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

                case NullReferenceException:
                case InvalidOperationException:
                case ArgumentNullException:
                    message = $"Errore applicativo: {ex.Message}";
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(message))
                        message = $"Errore generico: {ex.Message}";
                    break;
            }

            ErrorMessage = message;
            Logger?.LogError(ex, "{ErrorMessage}", message);

            if (NotificationDialog != null)
                await NotificationDialog.ShowNotification(message, "OK", Color.Error, "Error");
        }
    }
}
