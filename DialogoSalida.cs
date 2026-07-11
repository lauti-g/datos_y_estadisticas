using System;
using System.Drawing;
using System.Windows.Forms;

namespace datos_y_estadisticas
{
    // Ventana de confirmación al salir con cambios sin guardar.
    // Para salir, el usuario tiene que ESCRIBIR "si" (el botón Salir se habilita solo en ese caso)
    // o presionar "Cancelar" para volver y seguir trabajando.
    // Se construye 100% por código (no usa el diseñador) para mantenerlo autocontenido.
    public class DialogoSalida : Form
    {
        private readonly TextBox _txtConfirmacion;
        private readonly Button _btnSalir;

        public DialogoSalida()
        {
            // ----- Configuración de la ventana -----
            Text = "Salir sin guardar";
            // Ventana fija, centrada, sin maximizar/minimizar.
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 200);
            BackColor = Tema.FondoDatos;
            ForeColor = Tema.Blanco;
            Font = new Font("Segoe UI", 10F);

            // ----- Mensaje principal -----
            var lblMensaje = new Label
            {
                Text = "Tenés cambios sin guardar.\n¿Seguro que querés salir sin guardar?",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 20),
                Size = new Size(380, 50),
                ForeColor = Tema.Blanco
            };

            // ----- Instrucción -----
            var lblInstruccion = new Label
            {
                Text = "Para confirmar, escribí \"si\":",
                AutoSize = true,
                Location = new Point(20, 85),
                ForeColor = Tema.Dorado
            };

            // ----- Caja de texto donde se escribe "si" -----
            _txtConfirmacion = new TextBox
            {
                Location = new Point(20, 110),
                Size = new Size(380, 28),
                BackColor = Tema.PanelDatos,
                ForeColor = Tema.Blanco,
                BorderStyle = BorderStyle.FixedSingle
            };

            // ----- Botón Salir (sale del programa). Arranca deshabilitado. -----
            _btnSalir = new Button
            {
                Text = "Salir sin guardar",
                Location = new Point(20, 150),
                Size = new Size(180, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.AzulAccent,
                ForeColor = Tema.Blanco,
                Enabled = false,
                // DialogResult.OK = "el usuario confirmó salir".
                DialogResult = DialogResult.OK
            };
            _btnSalir.FlatAppearance.BorderColor = Tema.Dorado;

            // Cada vez que cambia el texto, reviso si dice "si" para habilitar el botón Salir.
            // (Se suscribe acá, una vez que _btnSalir ya existe.)
            _txtConfirmacion.TextChanged += (s, e) =>
            {
                _btnSalir.Enabled = EscribioSi();
            };

            // ----- Botón Cancelar (vuelve y NO cierra el programa) -----
            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(220, 150),
                Size = new Size(180, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.PanelDatos,
                ForeColor = Tema.Blanco,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderColor = Tema.AzulBorde;

            // Enter dispara Salir (si está habilitado); Escape dispara Cancelar.
            AcceptButton = _btnSalir;
            CancelButton = btnCancelar;

            // Agrego todos los controles a la ventana.
            Controls.Add(lblMensaje);
            Controls.Add(lblInstruccion);
            Controls.Add(_txtConfirmacion);
            Controls.Add(_btnSalir);
            Controls.Add(btnCancelar);
        }

        // Devuelve true solo si el texto, sin espacios y en minúsculas, es exactamente "si".
        private bool EscribioSi()
        {
            return _txtConfirmacion.Text.Trim().ToLowerInvariant() == "si";
        }
    }
}
