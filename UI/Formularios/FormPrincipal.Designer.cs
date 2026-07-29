using datos_y_estadisticas.UI.Controles; // TabControlOscuro y DateTimePickerOscuro

namespace datos_y_estadisticas.UI.Formularios
{
    partial class FormPrincipal
    {
        /// <summary>
        ///  Variable del diseñador (no tocar).
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Libera los recursos al cerrar el formulario.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Construye todos los controles del formulario.
        ///  (Los gráficos LiveCharts se agregan por código en FormPrincipal.cs.)
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPrincipal));
            tab = new TabControlOscuro();
            tabDatos = new TabPage();
            tabla = new DataGridView();
            nombreColumna = new DataGridViewTextBoxColumn();
            edadColumna = new DataGridViewTextBoxColumn();
            celularColumna = new DataGridViewTextBoxColumn();
            localidadColumna = new DataGridViewTextBoxColumn();
            mIC = new DataGridViewComboBoxColumn();
            borrarColumna = new DataGridViewButtonColumn();
            panelTopDatos = new Panel();
            BotonSalirModoBusqueda = new Button();
            btnBuscar = new Button();
            buscador = new TextBox();
            btnGuardar = new Button();
            dtpFecha = new DateTimePickerOscuro();
            lblFecha = new Label();
            btnImportar = new Button();
            tabEstadisticas = new TabPage();
            panelScroll = new Panel();
            tlpGraficos = new TableLayoutPanel();
            lblEdades = new Label();
            panelEdades = new Panel();
            panelMotivos = new Panel();
            lblLocalidades = new Label();
            panelLocalidades = new Panel();
            lblMotivos = new Label();
            panelTopEst = new Panel();
            dtpEstadisticas = new DateTimePickerOscuro();
            btnSemana = new Button();
            btnMes = new Button();
            btnTresMeses = new Button();
            btnTodo = new Button();
            PanelTop = new Panel();
            BotonMinimizar = new Button();
            BotonMinimizarTamaño = new Button();
            BotonMaximizar = new Button();
            BotonCerrar = new Button();
            PanelContenedor = new Panel();
            tab.SuspendLayout();
            tabDatos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tabla).BeginInit();
            panelTopDatos.SuspendLayout();
            tabEstadisticas.SuspendLayout();
            panelScroll.SuspendLayout();
            tlpGraficos.SuspendLayout();
            panelTopEst.SuspendLayout();
            PanelTop.SuspendLayout();
            PanelContenedor.SuspendLayout();
            SuspendLayout();
            // 
            // tab
            // 
            tab.Alignment = TabAlignment.Left;
            tab.Controls.Add(tabDatos);
            tab.Controls.Add(tabEstadisticas);
            tab.Cursor = Cursors.Hand;
            tab.Dock = DockStyle.Fill;
            tab.ItemSize = new Size(170, 44);
            tab.Location = new Point(0, 0);
            tab.Multiline = true;
            tab.Name = "tab";
            tab.SelectedIndex = 0;
            tab.Size = new Size(1093, 590);
            tab.SizeMode = TabSizeMode.Fixed;
            tab.TabIndex = 0;
            tab.SelectedIndexChanged += tab_SelectedIndexChanged;
            // 
            // tabDatos
            // 
            tabDatos.Controls.Add(tabla);
            tabDatos.Controls.Add(panelTopDatos);
            tabDatos.Location = new Point(48, 4);
            tabDatos.Name = "tabDatos";
            tabDatos.Size = new Size(1041, 582);
            tabDatos.TabIndex = 0;
            tabDatos.Text = "Datos";
            // 
            // tabla
            // 
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.Columns.AddRange(new DataGridViewColumn[] { nombreColumna, edadColumna, celularColumna, localidadColumna, mIC, borrarColumna });
            tabla.Dock = DockStyle.Fill;
            tabla.Location = new Point(0, 52);
            tabla.MultiSelect = false;
            tabla.Name = "tabla";
            tabla.Size = new Size(1041, 530);
            tabla.TabIndex = 0;
            // 
            // nombreColumna
            // 
            nombreColumna.HeaderText = "Nombre";
            nombreColumna.Name = "nombreColumna";
            // 
            // edadColumna
            // 
            edadColumna.FillWeight = 50F;
            edadColumna.HeaderText = "Edad";
            edadColumna.Name = "edadColumna";
            // 
            // celularColumna
            // 
            celularColumna.HeaderText = "Celular";
            celularColumna.Name = "celularColumna";
            // 
            // localidadColumna
            // 
            localidadColumna.HeaderText = "Localidad";
            localidadColumna.Name = "localidadColumna";
            // 
            // mIC
            // 
            mIC.HeaderText = "Motivo de ingreso";
            mIC.Items.AddRange(new object[] { "Redes", "Recomendacion", "Volantes", "Otro" });
            mIC.Name = "mIC";
            // 
            // borrarColumna
            // 
            borrarColumna.FillWeight = 50F;
            borrarColumna.HeaderText = "Limpiar";
            borrarColumna.Name = "borrarColumna";
            borrarColumna.Text = "Borrar";
            borrarColumna.UseColumnTextForButtonValue = true;
            // 
            // panelTopDatos
            // 
            panelTopDatos.Controls.Add(BotonSalirModoBusqueda);
            panelTopDatos.Controls.Add(btnBuscar);
            panelTopDatos.Controls.Add(buscador);
            panelTopDatos.Controls.Add(btnGuardar);
            panelTopDatos.Controls.Add(dtpFecha);
            panelTopDatos.Controls.Add(lblFecha);
            panelTopDatos.Controls.Add(btnImportar);
            panelTopDatos.Dock = DockStyle.Top;
            panelTopDatos.Location = new Point(0, 0);
            panelTopDatos.Name = "panelTopDatos";
            panelTopDatos.Size = new Size(1041, 52);
            panelTopDatos.TabIndex = 1;
            // 
            // BotonSalirModoBusqueda
            // 
            BotonSalirModoBusqueda.Anchor = AnchorStyles.Right;
            BotonSalirModoBusqueda.BackColor = Color.FromArgb(46, 90, 136);
            BotonSalirModoBusqueda.BackgroundImage = (Image)resources.GetObject("BotonSalirModoBusqueda.BackgroundImage");
            BotonSalirModoBusqueda.BackgroundImageLayout = ImageLayout.Zoom;
            BotonSalirModoBusqueda.FlatAppearance.BorderColor = Color.FromArgb(201, 162, 39);
            BotonSalirModoBusqueda.FlatAppearance.BorderSize = 0;
            BotonSalirModoBusqueda.FlatStyle = FlatStyle.Flat;
            BotonSalirModoBusqueda.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BotonSalirModoBusqueda.ForeColor = Color.FromArgb(201, 162, 39);
            BotonSalirModoBusqueda.Location = new Point(990, 13);
            BotonSalirModoBusqueda.Margin = new Padding(0);
            BotonSalirModoBusqueda.Name = "BotonSalirModoBusqueda";
            BotonSalirModoBusqueda.Size = new Size(42, 27);
            BotonSalirModoBusqueda.TabIndex = 6;
            BotonSalirModoBusqueda.TextImageRelation = TextImageRelation.ImageAboveText;
            BotonSalirModoBusqueda.UseVisualStyleBackColor = false;
            BotonSalirModoBusqueda.Visible = false;
            BotonSalirModoBusqueda.Click += BotonSalirModoBusqueda_Click;
            // 
            // btnBuscar
            // 
            btnBuscar.Anchor = AnchorStyles.Right;
            btnBuscar.BackColor = Color.FromArgb(46, 90, 136);
            btnBuscar.FlatAppearance.BorderColor = Color.FromArgb(201, 162, 39);
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.FlatStyle = FlatStyle.Flat;
            btnBuscar.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnBuscar.ForeColor = Color.FromArgb(201, 162, 39);
            btnBuscar.Location = new Point(990, 13);
            btnBuscar.Margin = new Padding(0);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(42, 27);
            btnBuscar.TabIndex = 4;
            btnBuscar.Text = "🔍";
            btnBuscar.TextImageRelation = TextImageRelation.ImageAboveText;
            btnBuscar.UseVisualStyleBackColor = false;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // buscador
            // 
            buscador.Anchor = AnchorStyles.Right;
            buscador.BackColor = Color.FromArgb(46, 90, 136);
            buscador.BorderStyle = BorderStyle.FixedSingle;
            buscador.Cursor = Cursors.IBeam;
            buscador.Font = new Font("Segoe UI", 12F);
            buscador.ForeColor = Color.FromArgb(201, 162, 39);
            buscador.ImeMode = ImeMode.Katakana;
            buscador.Location = new Point(776, 14);
            buscador.Name = "buscador";
            buscador.PlaceholderText = "Buscador";
            buscador.Size = new Size(211, 29);
            buscador.TabIndex = 3;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(12, 10);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(120, 32);
            btnGuardar.TabIndex = 0;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // dtpFecha
            // 
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Location = new Point(150, 14);
            dtpFecha.Name = "dtpFecha";
            dtpFecha.Size = new Size(140, 23);
            dtpFecha.TabIndex = 1;
            // 
            // lblFecha
            // 
            lblFecha.AutoSize = true;
            lblFecha.Font = new Font("Segoe UI", 12F);
            lblFecha.Location = new Point(305, 14);
            lblFecha.Name = "lblFecha";
            lblFecha.Size = new Size(38, 21);
            lblFecha.TabIndex = 2;
            lblFecha.Text = "Hoy";
            // 
            // btnImportar
            // 
            btnImportar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnImportar.Location = new Point(536, 10);
            btnImportar.Name = "btnImportar";
            btnImportar.Size = new Size(160, 32);
            btnImportar.TabIndex = 5;
            btnImportar.Text = "Importar Excel";
            btnImportar.UseVisualStyleBackColor = true;
            btnImportar.Click += btnImportar_Click;
            // 
            // tabEstadisticas
            // 
            tabEstadisticas.Controls.Add(panelScroll);
            tabEstadisticas.Controls.Add(panelTopEst);
            tabEstadisticas.Location = new Point(48, 4);
            tabEstadisticas.Name = "tabEstadisticas";
            tabEstadisticas.Size = new Size(1041, 582);
            tabEstadisticas.TabIndex = 1;
            tabEstadisticas.Text = "Estadisticas";
            // 
            // panelScroll
            // 
            panelScroll.AutoScroll = true;
            panelScroll.Controls.Add(tlpGraficos);
            panelScroll.Dock = DockStyle.Fill;
            panelScroll.Location = new Point(0, 52);
            panelScroll.Name = "panelScroll";
            panelScroll.Size = new Size(1041, 530);
            panelScroll.TabIndex = 0;
            // 
            // tlpGraficos
            // 
            tlpGraficos.AutoSize = true;
            tlpGraficos.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpGraficos.ColumnCount = 4;
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tlpGraficos.Controls.Add(lblEdades, 0, 0);
            tlpGraficos.Controls.Add(panelEdades, 0, 1);
            tlpGraficos.Controls.Add(panelMotivos, 2, 1);
            tlpGraficos.Controls.Add(lblLocalidades, 1, 2);
            tlpGraficos.Controls.Add(panelLocalidades, 1, 3);
            tlpGraficos.Controls.Add(lblMotivos, 2, 0);
            tlpGraficos.Dock = DockStyle.Top;
            tlpGraficos.Location = new Point(0, 0);
            tlpGraficos.Name = "tlpGraficos";
            tlpGraficos.RowCount = 4;
            tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Absolute, 360F));
            tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Absolute, 360F));
            tlpGraficos.Size = new Size(1024, 792);
            tlpGraficos.TabIndex = 0;
            // 
            // lblEdades
            // 
            tlpGraficos.SetColumnSpan(lblEdades, 2);
            lblEdades.Dock = DockStyle.Top;
            lblEdades.Font = new Font("Segoe UI Semibold", 12F);
            lblEdades.Location = new Point(0, 0);
            lblEdades.Margin = new Padding(0, 0, 3, 0);
            lblEdades.Name = "lblEdades";
            lblEdades.Size = new Size(508, 33);
            lblEdades.TabIndex = 4;
            lblEdades.Text = "Edades";
            lblEdades.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelEdades
            // 
            tlpGraficos.SetColumnSpan(panelEdades, 2);
            panelEdades.Dock = DockStyle.Fill;
            panelEdades.Location = new Point(0, 36);
            panelEdades.Margin = new Padding(0, 0, 3, 3);
            panelEdades.Name = "panelEdades";
            panelEdades.Size = new Size(508, 357);
            panelEdades.TabIndex = 5;
            // 
            // panelMotivos
            // 
            tlpGraficos.SetColumnSpan(panelMotivos, 2);
            panelMotivos.Dock = DockStyle.Fill;
            panelMotivos.Location = new Point(514, 36);
            panelMotivos.Margin = new Padding(3, 0, 0, 3);
            panelMotivos.Name = "panelMotivos";
            panelMotivos.Size = new Size(510, 357);
            panelMotivos.TabIndex = 3;
            // 
            // lblLocalidades
            // 
            tlpGraficos.SetColumnSpan(lblLocalidades, 2);
            lblLocalidades.Dock = DockStyle.Fill;
            lblLocalidades.Font = new Font("Segoe UI Semibold", 12F);
            lblLocalidades.Location = new Point(156, 399);
            lblLocalidades.Margin = new Padding(3, 3, 3, 0);
            lblLocalidades.Name = "lblLocalidades";
            lblLocalidades.Size = new Size(710, 33);
            lblLocalidades.TabIndex = 0;
            lblLocalidades.Text = "Localidades";
            lblLocalidades.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelLocalidades
            // 
            tlpGraficos.SetColumnSpan(panelLocalidades, 2);
            panelLocalidades.Dock = DockStyle.Fill;
            panelLocalidades.Location = new Point(156, 432);
            panelLocalidades.Margin = new Padding(3, 0, 3, 3);
            panelLocalidades.Name = "panelLocalidades";
            panelLocalidades.Size = new Size(710, 357);
            panelLocalidades.TabIndex = 2;
            // 
            // lblMotivos
            // 
            tlpGraficos.SetColumnSpan(lblMotivos, 2);
            lblMotivos.Dock = DockStyle.Top;
            lblMotivos.Font = new Font("Segoe UI Semibold", 12F);
            lblMotivos.Location = new Point(514, 0);
            lblMotivos.Margin = new Padding(3, 0, 0, 0);
            lblMotivos.Name = "lblMotivos";
            lblMotivos.Size = new Size(510, 33);
            lblMotivos.TabIndex = 1;
            lblMotivos.Text = "Motivos de ingreso";
            lblMotivos.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelTopEst
            // 
            panelTopEst.Controls.Add(dtpEstadisticas);
            panelTopEst.Controls.Add(btnSemana);
            panelTopEst.Controls.Add(btnMes);
            panelTopEst.Controls.Add(btnTresMeses);
            panelTopEst.Controls.Add(btnTodo);
            panelTopEst.Dock = DockStyle.Top;
            panelTopEst.Location = new Point(0, 0);
            panelTopEst.Margin = new Padding(3, 3, 3, 0);
            panelTopEst.Name = "panelTopEst";
            panelTopEst.Size = new Size(1041, 52);
            panelTopEst.TabIndex = 1;
            // 
            // dtpEstadisticas
            // 
            dtpEstadisticas.CalendarMonthBackground = SystemColors.HotTrack;
            dtpEstadisticas.Format = DateTimePickerFormat.Short;
            dtpEstadisticas.Location = new Point(12, 14);
            dtpEstadisticas.Name = "dtpEstadisticas";
            dtpEstadisticas.Size = new Size(140, 23);
            dtpEstadisticas.TabIndex = 0;
            // 
            // btnSemana
            // 
            btnSemana.Location = new Point(168, 10);
            btnSemana.Name = "btnSemana";
            btnSemana.Size = new Size(190, 32);
            btnSemana.TabIndex = 1;
            btnSemana.Text = "Mostrar semana completa";
            btnSemana.UseVisualStyleBackColor = true;
            // 
            // btnMes
            // 
            btnMes.Location = new Point(370, 10);
            btnMes.Name = "btnMes";
            btnMes.Size = new Size(190, 32);
            btnMes.TabIndex = 2;
            btnMes.Text = "Mostrar mes completo";
            btnMes.UseVisualStyleBackColor = true;
            // 
            // btnTresMeses
            // 
            btnTresMeses.Location = new Point(572, 10);
            btnTresMeses.Name = "btnTresMeses";
            btnTresMeses.Size = new Size(190, 32);
            btnTresMeses.TabIndex = 3;
            btnTresMeses.Text = "Mostrar 3 meses completos";
            btnTresMeses.UseVisualStyleBackColor = true;
            // 
            // btnTodo
            // 
            btnTodo.Location = new Point(774, 10);
            btnTodo.Name = "btnTodo";
            btnTodo.Size = new Size(190, 32);
            btnTodo.TabIndex = 4;
            btnTodo.Text = "Mostrar todo";
            btnTodo.UseVisualStyleBackColor = true;
            // 
            // PanelTop
            // 
            PanelTop.BackColor = Color.FromArgb(13, 27, 42);
            PanelTop.Controls.Add(BotonMinimizar);
            PanelTop.Controls.Add(BotonMinimizarTamaño);
            PanelTop.Controls.Add(BotonMaximizar);
            PanelTop.Controls.Add(BotonCerrar);
            PanelTop.Dock = DockStyle.Top;
            PanelTop.Location = new Point(0, 0);
            PanelTop.Name = "PanelTop";
            PanelTop.Size = new Size(1093, 30);
            PanelTop.TabIndex = 1;
            PanelTop.MouseDown += PanelTop_MouseDown;
            // 
            // BotonMinimizar
            // 
            BotonMinimizar.BackColor = Color.FromArgb(13, 27, 42);
            BotonMinimizar.BackgroundImage = (Image)resources.GetObject("BotonMinimizar.BackgroundImage");
            BotonMinimizar.BackgroundImageLayout = ImageLayout.Zoom;
            BotonMinimizar.Cursor = Cursors.Hand;
            BotonMinimizar.Dock = DockStyle.Right;
            BotonMinimizar.FlatAppearance.BorderColor = SystemColors.ControlDark;
            BotonMinimizar.FlatAppearance.BorderSize = 0;
            BotonMinimizar.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 90, 136);
            BotonMinimizar.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 90, 136);
            BotonMinimizar.FlatStyle = FlatStyle.Flat;
            BotonMinimizar.Location = new Point(953, 0);
            BotonMinimizar.Name = "BotonMinimizar";
            BotonMinimizar.Size = new Size(35, 30);
            BotonMinimizar.TabIndex = 3;
            BotonMinimizar.UseVisualStyleBackColor = false;
            BotonMinimizar.Click += BotonMinimizar_Click;
            // 
            // BotonMinimizarTamaño
            // 
            BotonMinimizarTamaño.BackColor = Color.FromArgb(13, 27, 42);
            BotonMinimizarTamaño.BackgroundImage = (Image)resources.GetObject("BotonMinimizarTamaño.BackgroundImage");
            BotonMinimizarTamaño.BackgroundImageLayout = ImageLayout.Zoom;
            BotonMinimizarTamaño.Cursor = Cursors.Hand;
            BotonMinimizarTamaño.Dock = DockStyle.Right;
            BotonMinimizarTamaño.FlatAppearance.BorderColor = SystemColors.ControlDark;
            BotonMinimizarTamaño.FlatAppearance.BorderSize = 0;
            BotonMinimizarTamaño.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 90, 136);
            BotonMinimizarTamaño.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 90, 136);
            BotonMinimizarTamaño.FlatStyle = FlatStyle.Flat;
            BotonMinimizarTamaño.Location = new Point(988, 0);
            BotonMinimizarTamaño.Name = "BotonMinimizarTamaño";
            BotonMinimizarTamaño.Size = new Size(35, 30);
            BotonMinimizarTamaño.TabIndex = 2;
            BotonMinimizarTamaño.UseVisualStyleBackColor = false;
            BotonMinimizarTamaño.Visible = false;
            BotonMinimizarTamaño.Click += BotonMinimizarTamaño_Click;
            // 
            // BotonMaximizar
            // 
            BotonMaximizar.BackColor = Color.FromArgb(13, 27, 42);
            BotonMaximizar.BackgroundImage = (Image)resources.GetObject("BotonMaximizar.BackgroundImage");
            BotonMaximizar.BackgroundImageLayout = ImageLayout.Zoom;
            BotonMaximizar.Cursor = Cursors.Hand;
            BotonMaximizar.Dock = DockStyle.Right;
            BotonMaximizar.FlatAppearance.BorderColor = SystemColors.ControlDark;
            BotonMaximizar.FlatAppearance.BorderSize = 0;
            BotonMaximizar.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 90, 136);
            BotonMaximizar.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 90, 136);
            BotonMaximizar.FlatStyle = FlatStyle.Flat;
            BotonMaximizar.Location = new Point(1023, 0);
            BotonMaximizar.Name = "BotonMaximizar";
            BotonMaximizar.Size = new Size(35, 30);
            BotonMaximizar.TabIndex = 1;
            BotonMaximizar.UseVisualStyleBackColor = false;
            BotonMaximizar.Click += BotonMaximizar_Click;
            // 
            // BotonCerrar
            // 
            BotonCerrar.BackColor = Color.FromArgb(13, 27, 42);
            BotonCerrar.BackgroundImage = (Image)resources.GetObject("BotonCerrar.BackgroundImage");
            BotonCerrar.BackgroundImageLayout = ImageLayout.Zoom;
            BotonCerrar.Cursor = Cursors.Hand;
            BotonCerrar.Dock = DockStyle.Right;
            BotonCerrar.FlatAppearance.BorderColor = SystemColors.ControlDark;
            BotonCerrar.FlatAppearance.BorderSize = 0;
            BotonCerrar.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 90, 136);
            BotonCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 90, 136);
            BotonCerrar.FlatStyle = FlatStyle.Flat;
            BotonCerrar.Location = new Point(1058, 0);
            BotonCerrar.Name = "BotonCerrar";
            BotonCerrar.Size = new Size(35, 30);
            BotonCerrar.TabIndex = 0;
            BotonCerrar.UseVisualStyleBackColor = false;
            BotonCerrar.Click += BotonCerrar_Click;
            // 
            // PanelContenedor
            // 
            PanelContenedor.Controls.Add(tab);
            PanelContenedor.Dock = DockStyle.Fill;
            PanelContenedor.Location = new Point(0, 30);
            PanelContenedor.Name = "PanelContenedor";
            PanelContenedor.Size = new Size(1093, 590);
            PanelContenedor.TabIndex = 2;
            // 
            // FormPrincipal
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1093, 620);
            Controls.Add(PanelContenedor);
            Controls.Add(PanelTop);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormPrincipal";
            Text = "Datos y Estadísticas";
            FormClosing += FormPrincipal_FormClosing;
            Load += FormPrincipal_Load;
            tab.ResumeLayout(false);
            tabDatos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)tabla).EndInit();
            panelTopDatos.ResumeLayout(false);
            panelTopDatos.PerformLayout();
            tabEstadisticas.ResumeLayout(false);
            panelScroll.ResumeLayout(false);
            panelScroll.PerformLayout();
            tlpGraficos.ResumeLayout(false);
            panelTopEst.ResumeLayout(false);
            PanelTop.ResumeLayout(false);
            PanelContenedor.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TabPage tabDatos;
        private TabPage tabEstadisticas;

        private DataGridView tabla;

        private Panel panelTopDatos;
        private Button btnGuardar;
        private Button btnImportar;
        private DateTimePickerOscuro dtpFecha;
        private Label lblFecha;

        private TableLayoutPanel tlpGraficos;
        private Label lblLocalidades;
        private Label lblMotivos;
        private Label lblEdades;          // TAREA 1
        private Panel panelLocalidades;
        private Panel panelMotivos;
        private Panel panelEdades;        // TAREA 1

        private Panel panelScroll;        // contenedor con barra de scroll
        private Panel panelTopEst;
        private DateTimePickerOscuro dtpEstadisticas;
        private Button btnSemana;
        private Button btnMes;
        private Button btnTresMeses;   // botón "últimos 3 meses"
        private Button btnTodo;        // botón "mostrar todo" (estadísticas de TODA la base)
        public Button btnBuscar;
        public TextBox buscador;
        private DataGridViewTextBoxColumn nombreColumna;
        private DataGridViewTextBoxColumn edadColumna;
        private DataGridViewTextBoxColumn celularColumna;
        private DataGridViewTextBoxColumn localidadColumna;
        private DataGridViewComboBoxColumn mIC;
        private DataGridViewButtonColumn borrarColumna;
        private Panel PanelTop;
        private Panel PanelContenedor;
        public TabControlOscuro tab;
        private Button BotonCerrar;
        private Button BotonMinimizar;
        private Button BotonMinimizarTamaño;
        private Button BotonMaximizar;
        public Button BotonSalirModoBusqueda;
    }
}