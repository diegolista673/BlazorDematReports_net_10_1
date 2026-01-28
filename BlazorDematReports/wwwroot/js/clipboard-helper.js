// Clipboard Helper con fallback multipli
window.clipboardHelper = {
    // Copia testo nella clipboard con fallback
    copyText: async function (text) {
        try {
            // Metodo 1: Navigator Clipboard API (moderno, richiede HTTPS o localhost)
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return { success: true, method: 'clipboard' };
            }

            // Metodo 2: execCommand (deprecato ma funziona ovunque)
            if (document.execCommand) {
                const textArea = document.createElement("textarea");
                textArea.value = text;
                textArea.style.position = "fixed";
                textArea.style.left = "-999999px";
                textArea.style.top = "-999999px";
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                
                const success = document.execCommand('copy');
                document.body.removeChild(textArea);
                
                if (success) {
                    return { success: true, method: 'execCommand' };
                }
            }

            // Metodo 3: Selezione manuale (fallback finale)
            const textArea = document.createElement("textarea");
            textArea.value = text;
            textArea.style.position = "fixed";
            textArea.style.top = "0";
            textArea.style.left = "0";
            textArea.style.width = "2em";
            textArea.style.height = "2em";
            textArea.style.padding = "0";
            textArea.style.border = "none";
            textArea.style.outline = "none";
            textArea.style.boxShadow = "none";
            textArea.style.background = "transparent";
            document.body.appendChild(textArea);
            textArea.select();
            
            try {
                document.execCommand('copy');
                document.body.removeChild(textArea);
                return { success: true, method: 'manual' };
            } catch (err) {
                document.body.removeChild(textArea);
                throw err;
            }

        } catch (error) {
            console.error('Errore copia clipboard:', error);
            return { 
                success: false, 
                error: error.message || 'Errore sconosciuto',
                details: error.toString()
            };
        }
    }
};
