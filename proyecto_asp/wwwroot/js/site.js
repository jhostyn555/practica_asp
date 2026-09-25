// ============================================================
//  EMPANADAS PAMELITA - site.js
//  Módulo global: Web Speech API + helpers de búsqueda
// ============================================================

// ── 1. BÚSQUEDA POR VOZ (Web Speech API) ────────────────────
(function () {
    'use strict';

    const SpeechRecognition =
        window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
        document.addEventListener('DOMContentLoaded', () => {
            document.querySelectorAll('.btn-voice').forEach(btn => {
                btn.style.display = 'none';
            });
        });
        return;
    }

    let currentRecognition = null;
    let manualStop = false;
    let finalTranscript = '';
    let toastTimeout = null;

    // Muestra retroalimentación visual amigable cerca del buscador
    function showVoiceToast(inputGroup, message, type) {
        if (!inputGroup) return;
        if (toastTimeout) clearTimeout(toastTimeout);

        // Remover toast previo si existe
        const prevToast = inputGroup.parentElement.querySelector('.voice-feedback-toast');
        if (prevToast) prevToast.remove();

        const toast = document.createElement('div');
        toast.className = `voice-feedback-toast voice-feedback-${type}`;
        toast.innerHTML = message;

        inputGroup.insertAdjacentElement('afterend', toast);

        // Ocultar automáticamente
        const duration = type === 'error' ? 5000 : 3000;
        toastTimeout = setTimeout(() => {
            toast.remove();
        }, duration);
    }

    function removeVoiceToast(inputGroup) {
        if (!inputGroup) return;
        const prevToast = inputGroup.parentElement.querySelector('.voice-feedback-toast');
        if (prevToast) prevToast.remove();
    }

    function updateInput(targetInput, text) {
        targetInput.value = text;
        targetInput.dispatchEvent(new Event('input', { bubbles: true }));
        targetInput.dispatchEvent(new Event('keyup', { bubbles: true }));
        targetInput.dispatchEvent(new Event('change', { bubbles: true }));
        if (window.jQuery) {
            window.jQuery(targetInput).trigger('input').trigger('keyup').trigger('change');
        }
    }

    function autoSearch(targetInput) {
        const text = targetInput.value.trim();
        if (!text) return;

        const form = targetInput.closest('form');
        if (form && !targetInput.classList.contains('admin-search-input')) {
            if (typeof form.requestSubmit === 'function') {
                form.requestSubmit();
            } else {
                form.submit();
            }
        }
    }

    function stopUI(btn) {
        btn.classList.remove('btn-voice--listening');
        btn.title = 'Buscar por voz';
    }

    function createRecognition(btn, targetInput) {
        const recognition = new SpeechRecognition();

        // Español estándar con fallback (es-419 Latinoamérica es el más compatible con los servidores de voz de Google)
        const userLang = (navigator.language || '').toLowerCase();
        recognition.lang = userLang.startsWith('es') ? userLang : 'es-419';

        recognition.continuous = false;
        recognition.interimResults = true; // Permite ver las palabras en tiempo real mientras se habla

        const inputGroup = btn.closest('.input-group');

        recognition.onstart = () => {
            showVoiceToast(inputGroup, '<i class="bi bi-mic-fill text-danger"></i> Escuchando... habla ahora', 'listening');
        };

        recognition.onresult = (event) => {
            let interim = '';
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    finalTranscript += transcript + ' ';
                } else {
                    interim += transcript;
                }
            }
            const completeText = (finalTranscript + interim).trim();
            if (completeText) {
                updateInput(targetInput, completeText);
            }
        };

        recognition.onerror = (event) => {
            console.warn('Speech recognition error:', event.error);
            manualStop = true;

            let msg = '';
            if (event.error === 'not-allowed' || event.error === 'service-not-allowed') {
                msg = '<i class="bi bi-exclamation-triangle-fill"></i> Micrófono bloqueado. Permite el acceso al micrófono en el ícono 🔒 de la barra de direcciones.';
            } else if (event.error === 'network') {
                msg = '<i class="bi bi-wifi-off"></i> Error de red en el servicio de voz de Google. Revisa tu conexión.';
            } else if (event.error === 'no-speech') {
                msg = '<i class="bi bi-volume-mute"></i> No se detectó voz. Por favor intenta hablar de nuevo.';
            } else if (event.error === 'audio-capture') {
                msg = '<i class="bi bi-mic-mute"></i> No se encontró un micrófono disponible.';
            } else {
                msg = `<i class="bi bi-info-circle"></i> Error de reconocimiento: ${event.error}`;
            }

            showVoiceToast(inputGroup, msg, 'error');
        };

        recognition.onend = () => {
            stopUI(btn);
            currentRecognition = null;

            const captured = finalTranscript.trim();
            if (captured) {
                showVoiceToast(inputGroup, '<i class="bi bi-check-circle-fill text-success"></i> ¡Texto reconocido!', 'success');
                autoSearch(targetInput);
            } else if (!manualStop) {
                removeVoiceToast(inputGroup);
            }
            finalTranscript = '';
        };

        return recognition;
    }

    function startVoice(btn, targetInput) {
        const inputGroup = btn.closest('.input-group');

        // Verificación de contexto seguro (Web Speech API requiere HTTPS o localhost)
        if (window.isSecureContext === false && location.hostname !== 'localhost' && location.hostname !== '127.0.0.1') {
            showVoiceToast(inputGroup, '<i class="bi bi-shield-exclamation"></i> El reconocimiento de voz requiere conexión segura (HTTPS o localhost).', 'error');
            return;
        }

        if (currentRecognition) {
            manualStop = true;
            currentRecognition.stop();
            currentRecognition = null;
            stopUI(btn);
            removeVoiceToast(inputGroup);
            return;
        }

        manualStop = false;
        finalTranscript = '';
        currentRecognition = createRecognition(btn, targetInput);

        btn.classList.add('btn-voice--listening');
        btn.title = 'Escuchando… (haz clic para detener)';

        try {
            currentRecognition.start();
        } catch (e) {
            console.warn('Error al iniciar SpeechRecognition:', e);
            stopUI(btn);
            currentRecognition = null;
            showVoiceToast(inputGroup, '<i class="bi bi-exclamation-circle"></i> No se pudo iniciar el micrófono.', 'error');
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.body.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-voice');
            if (!btn) return;

            const targetId = btn.dataset.voiceFor;
            const targetInput = targetId
                ? document.getElementById(targetId)
                : btn.closest('.input-group')?.querySelector('input[type="text"], input:not([type])');

            if (!targetInput) return;
            startVoice(btn, targetInput);
        });
    });

})();

// ── 2. FILTRADO UNIVERSAL DE BÚSQUEDA ADMIN Y PRODUCTOS ─────
document.addEventListener('DOMContentLoaded', function () {
    const adminInputs = document.querySelectorAll('.admin-search-input');
    adminInputs.forEach(input => {
        const target = input.dataset.target;
        if (!target) return;

        const applyFilter = function () {
            const value = input.value.toLowerCase();
            document.querySelectorAll(target).forEach(el => {
                el.style.display = el.textContent.toLowerCase().includes(value)
                    ? ''
                    : 'none';
            });
        };

        input.addEventListener('input', applyFilter);
        input.addEventListener('keyup', applyFilter);
        input.addEventListener('change', applyFilter);
    });

    // Manejador para el botón "Buscar" explícito en la sección de productos
    const btnBuscarProductos = document.getElementById('btnBuscarProductos');
    if (btnBuscarProductos) {
        btnBuscarProductos.addEventListener('click', function () {
            const buscador = document.getElementById('buscadorProductos');
            if (!buscador) return;
            const value = buscador.value.toLowerCase();
            document.querySelectorAll('.producto-item').forEach(el => {
                el.style.display = el.textContent.toLowerCase().includes(value)
                    ? ''
                    : 'none';
            });
            // Desplazar suavemente a los productos si es necesario
            const targetGrid = document.querySelector('.producto-item');
            if (targetGrid) {
                targetGrid.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });
    }
});