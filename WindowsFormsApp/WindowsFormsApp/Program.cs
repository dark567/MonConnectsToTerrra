using MonConnectsToTerra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    static class Program
    {
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //    if (mutex.WaitOne(TimeSpan.Zero, true)) //ver 1.0
            //    {
            //        Application.EnableVisualStyles();
            //        Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new MainForm());
            //        mutex.ReleaseMutex();
            //    }
            //    else
            //    {
            //        MessageBox.Show("only one instance at a time");
            //    }

            if (mutex.WaitOne(TimeSpan.Zero, true)) //ver 2.0
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                mutex.ReleaseMutex();
            }
            else
            {
                // send our Win32 message to make the currently running instance
                // jump on top of all the other windows
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
        }
    }

    static class db_puth
    {
        public static string Value { get; set; }
    }
}

#region infa
//Сегодня я хотел провести рефакторинг некоторого кода, который запрещал моему приложению запускать несколько своих экземпляров.

//Ранее я использовал System.Diagnostics.Process для поиска экземпляра моего myapp.exe в списке процессов. Хотя это работает, это приносит много накладных расходов, и я хотел что-то более чистое.

//Зная, что я могу использовать мьютекс для этого (но никогда не делал этого раньше), я решил сократить свой код и упростить свою жизнь.

//В классе main моего приложения я создал статический файл с именем Mutex :

//static class Program
//{
//    static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
//    [STAThread]
//    ...
//}
//Наличие именованного мьютекса позволяет нам синхронизировать несколько потоков и процессов, и это просто волшебство, которое я ищу.

//Mutex.WaitOne имеет перегрузку, которая определяет время ожидания.Поскольку мы на самом деле не хотим синхронизировать наш код (больше просто проверьте, используется ли он в данный момент), мы используем перегрузку с двумя параметрами: Mutex.WaitOne(Timespan timeout, bool exitContext) . Подождите, один возвращает истину, если он может войти, и ложь, если это не так.В этом случае мы вообще не хотим ждать; Если наш мьютекс используется, пропустите его и двигайтесь дальше, поэтому мы передаем TimeSpan.Zero(ожидание 0 миллисекунд) и устанавливаем значение exitContext равным true, чтобы мы могли выйти из контекста синхронизации, прежде чем попытаться установить для него блокировку.Используя это, мы оборачиваем наш код Application.Run внутри чего-то вроде этого:

//static class Program
//{
//    static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
//    [STAThread]
//    static void Main()
//    {
//        if (mutex.WaitOne(TimeSpan.Zero, true))
//        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            Application.Run(new Form1());
//            mutex.ReleaseMutex();
//        }
//        else
//        {
//            MessageBox.Show("only one instance at a time");
//        }
//    }
//}
//Итак, если наше приложение работает, WaitOne вернет false, и мы получим окно сообщения.

//Вместо того, чтобы показывать окно сообщения, я решил использовать небольшой Win32, чтобы уведомить мой запущенный экземпляр о том, что кто-то забыл, что он уже запущен(подняв себя наверх всех остальных окон). Чтобы добиться этого, я использовал PostMessage для трансляции пользовательского сообщения в каждое окно(пользовательское сообщение было зарегистрировано в RegisterWindowMessage моим запущенным приложением, что означает, что только мое приложение знает, что это такое), после чего мой второй экземпляр завершается.Работающий экземпляр приложения получит это уведомление и обработает его. Чтобы сделать это, я переопределил WndProc в своей основной форме и прослушал свое пользовательское уведомление. Когда я получил это уведомление, я установил для свойства TopMost формы значение true, чтобы оно отображалось сверху.


//Вот что я закончил:


//Program.cs
//static class Program
//{
//    static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
//    [STAThread]
//    static void Main()
//    {
//        if (mutex.WaitOne(TimeSpan.Zero, true))
//        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            Application.Run(new Form1());
//            mutex.ReleaseMutex();
//        }
//        else
//        {
//            // send our Win32 message to make the currently running instance
//            // jump on top of all the other windows
//            NativeMethods.PostMessage(
//                (IntPtr)NativeMethods.HWND_BROADCAST,
//                NativeMethods.WM_SHOWME,
//                IntPtr.Zero,
//                IntPtr.Zero);
//        }
//    }
//}
//NativeMethods.cs
//// this class just wraps some Win32 stuff that we're going to use
//internal class NativeMethods
//{
//    public const int HWND_BROADCAST = 0xffff;
//    public static readonly int WM_SHOWME = RegisterWindowMessage("WM_SHOWME");
//    [DllImport("user32")]
//    public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
//    [DllImport("user32")]
//    public static extern int RegisterWindowMessage(string message);
//}
//Form1.cs(частичная лицевая сторона)
//public partial class Form1 : Form
//{
//    public Form1()
//    {
//        InitializeComponent();
//    }
//    protected override void WndProc(ref Message m)
//    {
//        if (m.Msg == NativeMethods.WM_SHOWME)
//        {
//            ShowMe();
//        }
//        base.WndProc(ref m);
//    }
//    private void ShowMe()
//    {
//        if (WindowState == FormWindowState.Minimized)
//        {
//            WindowState = FormWindowState.Normal;
//        }
//        // get our current "TopMost" value (ours will always be false though)
//        bool top = TopMost;
//        // make our form jump to the top of everything
//        TopMost = true;
//        // set it back to whatever it was
//        TopMost = top;
//    }
//}
#endregion
