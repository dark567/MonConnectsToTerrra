using FirebirdSql.Data.FirebirdClient;
using MonConnectsToTerra;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    public partial class MainForm : Form
    {
        FbConnection fb; //fb ссылается на соединение с нашей базой данных, по-этому она должна быть доступна всем методам нашего класса
        public string path_db;
        ToolStripLabel infolabel;
        ToolStripLabel datelabel;
        ToolStripLabel timelabel;
        ToolStripLabel typelabel;
        Timer timer;
        Boolean AdminMode = false;

        public MainForm()
        {
            InitializeComponent();

            this.Load += new EventHandler(Form1_Load);

            StatusLable();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_SHOWME)
            {
                ShowMe();
            }
            base.WndProc(ref m);
        }
        private void ShowMe()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            // get our current "TopMost" value (ours will always be false though)
            bool top = TopMost;
            // make our form jump to the top of everything
            TopMost = true;
            // set it back to whatever it was
            TopMost = top;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LicensyaCheck();
        }

        private void LicensyaCheck()
        {
            string curFile = @"keyfile.dat";
            if (!File.Exists(curFile)) Close();

            CryptoClass crypto = new CryptoClass();
            string date = crypto.GetDecodeKey(curFile).Substring(crypto.GetDecodeKey("keyfile.dat").IndexOf("|") + 1);
            if (DateTime.Parse(date).AddDays(1) <= DateTime.Now)
                try
                {
                    if (File.Exists(curFile))
                        File.Delete(curFile);
                }
                catch (IOException) { }
                finally
                { Close(); }

            int indexOfSubstring = this.Text.IndexOf(date);

            if (indexOfSubstring < 0)
                this.Text = this.Text + "......." + date;




        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            try
            {
                //Создание объекта, для работы с файлом
                INIManager manager = new INIManager(Application.StartupPath + "\\set.ini");
                //Получить значение по ключу name из секции main
                path_db = manager.GetPrivateString("connection", "db");
                db_puth.Value = path_db;

                File.AppendAllText(Application.StartupPath + @"\Event.log", "путь к db:" + db_puth.Value + "\n");
                //Записать значение по ключу age в секции main
                // manager.WritePrivateString("main", "age", "21");

                OnUserNameMessage(path_db);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ini не прочтен" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {

                string command = "select (select count(1) from SYS_SESSIONS t2 where SS.\"DATE_TIME\" >= t2.\"DATE_TIME\" and t2.IS_SERVICE_CONNECTION = 0) as N," +
                                            "DATE_TIME as \"Date Time\", " +
                                            //"CURRENT_CONNECTION,"+
                                            "(select MON$REMOTE_ADDRESS from mon$attachments where MON$ATTACHMENT_ID = SS.\"CURRENT_CONNECTION\") as \"REMOTE ADDRESS\"," +
                                            "USER_ID," +
                                            "(select Login from sec_users where ID = SS.\"USER_ID\")," +
                                            //"EMPLOYEE_ID, "+
                                            "(select CODE_NAME from dic_employee where ID = SS.\"EMPLOYEE_ID\") as \"CODE NAME\"" +
                                            //",IS_SERVICE_CONNECTION " +
                                            "from SYS_SESSIONS SS " +
                                            "where SS.IS_SERVICE_CONNECTION = 0" +
                                            "order by 1";

                //FBAdapter.Fill(Test(command), "dic_clients");

                DataSet fbds = Test(command);

                int count = fbds.Tables[0].Rows.Count; /*количество записей*/

                //fbds = fbds1;

                FbDatabaseInfo fb_inf = new FbDatabaseInfo(fb); //информация о БД
                                                                //пока у объекта БД не был вызван метод Open() - никакой информации о БД не получить, будет только ошибка
                                                                //MessageBox.Show("connect Info: " + fb_inf.ServerClass + "; " + fb_inf.ServerVersion); //выводим тип и версию используемого сервера Firebird

                typelabel.Text = "connect Info: " + fb_inf.ServerClass + "; " + fb_inf.ServerVersion + "; Rows: " + count;

                statusStrip1.Items.Add(typelabel);

                //this.dataGridView1.DataSource = fbds.Tables["dic_clients"].DefaultView;

                dataGridView1.DataSource = fbds.Tables["dic_clients"].DefaultView;
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                fb.Close();

                dataGridView1[0, dataGridView1.Rows.Count - 1].Value = "Total Count:";
                //dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = Color.Green;
                //dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.ForeColor = Color.Red;

            }
            catch (Exception ex)
            {
                File.AppendAllText(Application.StartupPath + @"\Event.log", "Не работает. Ошибка: " + ex.Message);
                MessageBox.Show("Не работает. Ошибка: " + ex.Message);
            }

        }

        private DataSet Test(string Command)
        {
            FbConnectionStringBuilder fb_con = new FbConnectionStringBuilder();
            fb_con.Charset = "UTF8"; //используемая кодировка
            fb_con.UserID = "SYSDBA"; //логин
            fb_con.Password = "masterkey"; //пароль
            fb_con.Database = db_puth.Value; //путь к файлу базы данных
            fb_con.ServerType = 0; //указываем тип сервера (0 - "полноценный Firebird" (classic или super server), 1 - встроенный (embedded))
            fb = new FbConnection(fb_con.ToString()); //передаем нашу строку подключения объекту класса FbConnection

            MonConnectsToTerra.Properties.Settings s = new MonConnectsToTerra.Properties.Settings();

            try
            {
                fb.Open(); //открываем БД
                FbCommand fbcommand = fb.CreateCommand();
                fbcommand.CommandType = CommandType.Text;
                fbcommand.Connection = fb;
                fbcommand.CommandText = Command;

                //  MessageBox.Show(fbcommand.CommandText);
                FbDataAdapter FBAdapter = new FbDataAdapter(fbcommand.CommandText, fbcommand.Connection);

                DataSet fbds = new DataSet("first_tab_DS");

                FBAdapter.Fill(fbds, "dic_clients");

                return fbds;

            }
            catch (Exception ex)
            {
                File.AppendAllText(Application.StartupPath + @"\Event.log", "Не работает. Ошибка: " + ex.Message);
                MessageBox.Show("Не работает. Ошибка: " + ex.Message);

                return null;
            }


        }

        private void OnUserNameMessage(string path_db)
        {
            int indexOfSubstring = this.Text.IndexOf("!");

            if (indexOfSubstring < 0)
                if (string.IsNullOrEmpty(path_db))
                    this.Text = "MonConnections";
                else
                    this.Text = "MonConnections - (" + path_db + ")";
        }

        private void StatusLable()
        {
            // toolStripStatusLabel1.Text = "Текущие дата и время";

            infolabel = new ToolStripLabel();
            infolabel.Text = "Current date and time:";

            datelabel = new ToolStripLabel();
            timelabel = new ToolStripLabel();
            typelabel = new ToolStripLabel();

            statusStrip1.Items.Add(infolabel);
            statusStrip1.Items.Add(datelabel);
            statusStrip1.Items.Add(timelabel);

            timer = new Timer() { Interval = 1000 };
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            datelabel.Text = DateTime.Now.ToLongDateString();
            timelabel.Text = DateTime.Now.ToLongTimeString();

            //TimeSpan diff = DateTime.Now;
            //double seconds = diff.TotalSeconds;

            //if (DateTime.Now.Second % 30 == 0)
            //{
            //    this.MainForm_Load(sender, e);
            //    LicensyaCheck();
            //}
        }

        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            LicensyaCheck();

            if (AdminMode)
            {
                int indexOfSubstring = this.Text.IndexOf("!");

                if (indexOfSubstring < 0)
                    this.Text = this.Text + "!";
            }
            else
            {
                int indexOfSubstring = this.Text.IndexOf("!");

                if (indexOfSubstring > 0)
                    this.Text = this.Text.Substring(0, this.Text.Length - 1);
            }

            string key = Convert.ToString(e.KeyData);
            if (key == "F5")
            {
                this.MainForm_Load(sender, e);
            }
        }

        private void dataGridView1_DataSourceChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name == "DATE_TIME" || column.Name == "LOGIN")
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    column.Width = column.Width; //This is important, otherwise the following line will nullify your previous command
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                    //column.Width = 100;
                }

                if (column.Name == "USER_ID")
                {
                    if (AdminMode)
                        column.Visible = true;
                    else
                        column.Visible = false;
                }

            }


            dataGridView1[0, dataGridView1.Rows.Count - 1].Value = "Total Sum";
            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = Color.Green;
            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.ForeColor = Color.Black;
            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.BackColor = Color.Green;
            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.ForeColor = Color.Black;

            int sum = 0;

            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1[1, i].Value != DBNull.Value)
                    sum++;//= (int)dataGridView1[2, i].Value;
            }
            if (sum >= 17 && sum < 18)
            {
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = Color.Yellow;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.ForeColor = Color.Black;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.BackColor = Color.Yellow;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.ForeColor = Color.Black;
            }
            if (sum >= 18)
            {
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = Color.Red;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.ForeColor = Color.Black;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.BackColor = Color.Red;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Style.ForeColor = Color.Black;
            }
            dataGridView1[1, dataGridView1.Rows.Count - 1].Value = sum;

        }


        /// <summary>
        /// Works but not needed yet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            //    Int32 selectedCellCount =
            //dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
            //    if (selectedCellCount > 0)
            //    {
            //        if (dataGridView1.AreAllCellsSelected(true))
            //        {
            //            MessageBox.Show("All cells are selected", "Selected Cells");
            //        }
            //        else
            //        {
            //            System.Text.StringBuilder sb =
            //                new System.Text.StringBuilder();

            //            for (int i = 0;
            //                i < selectedCellCount; i++)
            //            {
            //                //sb.Append("Row: ");
            //                //sb.Append(dataGridView1.SelectedCells[i].RowIndex
            //                //    .ToString());
            //                //sb.Append(", Column: ");
            //                //sb.Append(dataGridView1.SelectedCells[i].ColumnIndex
            //                //    .ToString());
            //                sb.Append("Content: ");
            //                sb.Append(dataGridView1[dataGridView1.SelectedCells[i].ColumnIndex, dataGridView1.SelectedCells[i].RowIndex].Value);
            //                sb.Append(Environment.NewLine);
            //            }

            //            //sb.Append("Total: " + selectedCellCount.ToString());

            //            MessageBox.Show(sb.ToString(), "Selected Cells");
            //        }

            //    }

        }

        private void AdminModeToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (AdminMode)
            {
                AdminMode = false;
            }
            else
            {
                AdminMode = true;
            }
        }

        private void DataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                DataGridViewCell c = (sender as DataGridView)[e.ColumnIndex, e.RowIndex];
                if (!c.Selected)
                {
                    c.DataGridView.ClearSelection();
                    c.DataGridView.CurrentCell = c;
                    c.Selected = true;
                }

                if (e.Button == MouseButtons.Right)
                {
                    DataGridViewCell clickedCell = (sender as DataGridView).Rows[e.RowIndex].Cells[e.ColumnIndex];

                    // Here you can do whatever you want with the cell
                    this.dataGridView1.CurrentCell = clickedCell;  // Select the clicked cell, for instance

                    // Get mouse position relative to the vehicles grid
                    var relativeMousePosition = dataGridView1.PointToClient(Cursor.Position);

                    // Show the context menu
                    if (AdminMode)
                    {
                        this.contextMenuStrip1.Show(dataGridView1, relativeMousePosition);
                    }
                }
            }
        }

        private void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string idParam = null;

            Int32 selectedCellCount =
        dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
            if (selectedCellCount > 0)
            {
                if (dataGridView1.AreAllCellsSelected(true))
                {
                    MessageBox.Show("All cells are selected", "Selected Cells");
                }
                else
                {
                    StringBuilder sb =
                        new StringBuilder();

                    for (int i = 0;
                        i < selectedCellCount; i++)
                    {
                        //sb.Append("Row: ");
                        //sb.Append(dataGridView1.SelectedCells[i].RowIndex
                        //    .ToString());
                        //sb.Append(", Column: ");
                        //sb.Append(dataGridView1.SelectedCells[i].ColumnIndex.ToString());

                        //sb.Append(dataGridView1.SelectedColumns[dataGridView1.SelectedCells[i].ColumnIndex].HeaderText.ToString()); //.SelectedColumns[0].HeaderText
                        sb.Append("Content: ");

                        sb.Append(dataGridView1[dataGridView1.SelectedCells[i].ColumnIndex, dataGridView1.SelectedCells[i].RowIndex].Value);
                        sb.Append(Environment.NewLine);

                        if (dataGridView1.SelectedCells[i].ColumnIndex.ToString() == "3")
                            idParam = dataGridView1[dataGridView1.SelectedCells[i].ColumnIndex, dataGridView1.SelectedCells[i].RowIndex].Value.ToString();
                    }

                    //sb.Append("Total: " + selectedCellCount.ToString());

                    //MessageBox.Show(sb.ToString(), "Selected Cells");

                    if (!string.IsNullOrEmpty(idParam))
                    {
                        string message = $"Are you sure that you would like to close this connection? {sb.ToString()}";
                        const string caption = "Connect Closing";
                        var result = MessageBox.Show(message, caption,
                                                     MessageBoxButtons.YesNo,
                                                     MessageBoxIcon.Question);

                        // If the no button was pressed ...
                        if (result == DialogResult.Yes)
                        {
                            CloseConnection(idParam);
                            this.MainForm_Load(sender, e);
                        }

                        if (result == DialogResult.No)
                        {
                            this.MainForm_Load(sender, e);
                        }
                    }
                }
            }

        }

        private void CloseConnection(string idParam = "")
        {
            string fbQuery = $"delete from SYS_SESSIONS where USER_ID = cast('{idParam}' as ID) AND (IS_SERVICE_CONNECTION = 0)";
            //MessageBox.Show(fbQuery);
            FbConnection fb = GetConnection();
            //fb.Open();
            FbCommand SelectSQL = new FbCommand(fbQuery, fb);

            FbTransaction fbt = fb.BeginTransaction();
            SelectSQL.Transaction = fbt;
            try
            {
                int count = SelectSQL.ExecuteNonQuery();
                //MessageBox.Show(SelectSQL.CommandText);
                MessageBox.Show($"Close successful:{count}","Infa", MessageBoxButtons.OK, MessageBoxIcon.Question);
                fbt.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("error" + ex.Message);
                fbt.Rollback();
            }
            finally
            {
                SelectSQL.Dispose();
                fb.Close();
            }
        }

        private FbConnection GetConnection()
        {
            string connectionString =
                "User=SYSDBA;" +
                "Password=masterkey;" +
                @"Database=" + path_db + ";" +
                "Charset=UTF8;" +
                "Pooling=true;" +
                "ServerType=0;";

            FbConnection conn = new FbConnection(connectionString.ToString());

            conn.Open();

            return conn;
        }

        private void DataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (!AdminMode)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenu m = new ContextMenu();
                    MenuItem Cut = new MenuItem();
                    Cut.Text = "&Cut"; //& = Shift + C
                    Cut.Click += new EventHandler(CutDo);
                    m.MenuItems.Add(Cut);

                    MenuItem Copy = new MenuItem();
                    Copy.Text = "&Copy"; //& = Shift + C
                    Copy.Click += new EventHandler(CopyDo);
                    m.MenuItems.Add(Copy);

                    MenuItem Paste = new MenuItem();
                    Paste.Text = "&Paste"; //& = Shift + P
                    Paste.Click += new EventHandler(PasteDo);
                    m.MenuItems.Add(Paste);

                    var relativeMousePosition = dataGridView1.PointToClient(Cursor.Position);

                    m.Show(dataGridView1, relativeMousePosition);

                }
            }
        }

        private void PasteDo(object sender, EventArgs e)
        {
            MessageBox.Show("Paste TODO");
        }

        private void CopyDo(object sender, EventArgs e)
        {
            MessageBox.Show("Copy TODO");
        }

        private void CutDo(object sender, EventArgs e)
        {
            MessageBox.Show("Cut TODO");
        }
    }
}
