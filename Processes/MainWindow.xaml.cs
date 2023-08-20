using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Management;
using System.Threading;
using System.Linq;
using System.Xml;

namespace Processes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //a constant that identifies the WM _ SETTEXT message
        const uint WM_SETTEXT = 0x0C;
        //import the SendMEssage function from 
        //the user32.dll library
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, int wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);
        /*a list, which will store objects,
        *describing child processes of the application*/
        List<Process> Processes = new List<Process>();
        /*a counter of the running processes*/
        int Counter = 0;
        public MainWindow()
        {
            InitializeComponent();
            LoadAvailableAssemblies();
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (AvailableAssemblies.SelectedItem != null)
                RunProcess(AvailableAssemblies.SelectedItem.ToString() ?? "");
        }

        private void OnStopClicked(object sender, EventArgs e) { }

        void LoadAvailableAssemblies()
        {
            var processPath = Environment.ProcessPath ?? "";
            //a file name of the current application's assembly
            string except = new FileInfo(processPath).Name;
            //get the file name without extension
            except = except.Substring(0, except.IndexOf("."));
            //get all the *.exe files from the home directory 
            string[] files = Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.exe");

            foreach (var file in files)
            {
                //get the file name
                string fileName = new FileInfo(file).Name;
                //*if the file name doesn’t contain the project’s *executable file name, it will be added to a list*/
                if (fileName.IndexOf(except) == -1) AvailableAssemblies.Items.Add(fileName);
            }
        }

        /*a method, which runs the process for an execution and 
 *saves an object that describes it*/
        void RunProcess(string AssamblyName)
        {
            //run the process on the base of the executable file
            Process proc = Process.Start(AssamblyName);;
            //add the process to a list
            Processes.Add(proc);
            /*check whether the newly created process became a 
             *child process in relation to the current one, and, 
             *if it is so,output MessageBox*/
            if (Process.GetCurrentProcess().Id == GetParentProcessId(proc.Id))
            {
                Trace.WriteLine(proc.ProcessName + " it is really a child process of the current process!");
            }
            /*specify that a process should generate events*/
            proc.EnableRaisingEvents = true;
            //add a handler for an event of the process 
            //completion
            proc.Exited += proc_Exited;
            /*set a new text to the main window of the child 
             *process*/
            try
            {
                if (FindWindow(AssamblyName, 1) == proc.MainWindowHandle)
                {
                    Trace.WriteLine("OK");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed: Process not OK!");
            }
            SetChildWindowText(proc.MainWindowHandle, "Child process #" + (++Counter));
            /*check whether we have run an instance of this 
             *application and if we haven’t done it, add it to 
             *the list of running applications*/
            if (!StartedAssemblies.Items.Contains(proc.ProcessName)) StartedAssemblies.Items.Add(proc.ProcessName);
            /*remove the application from the list of 
             *avaliable applications*/
            AvailableAssemblies.Items.Remove(AvailableAssemblies.SelectedItem);
        }

        /*a wrapping method to send the WM _ SETTEXT message*/
        void SetChildWindowText(IntPtr Handle, string text)
        {
            SendMessage(Handle, WM_SETTEXT, 0, text);
        }

        /*a method, which gets PID of the parent process (uses WMI)*/
        int GetParentProcessId(int Id)
        {
            int parentId = 0;
            using (ManagementObject obj = new ManagementObject("win32_process.handle=" + Id.ToString()))
            {
                obj.Get();
                parentId = Convert.ToInt32(obj["ParentProcessId"]);
            }
            return parentId;
        }

        /*Exited event handler of the Process class*/
        void proc_Exited(object sender, EventArgs e)
        {
            Process? proc = sender as Process;
            //remove the process from the list of running 
            //applications 
            if(proc != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    StartedAssemblies.Items.Remove(proc.ProcessName);
                    //add the process to the list of avaliable 
                    //applications 
                    AvailableAssemblies.Items.Add(proc.ProcessName);
                    //remove the process from the list of child 
                    //processes 
                    Processes.Remove(proc);
                });
            }
            //reduce the counter of child processes by 1
            Counter--;
            int index = 0;
            /*change the text for the main windows of all 
             *child processes*/
            foreach (var p in Processes)
            {
                SetChildWindowText(p.MainWindowHandle, "Child process #" + ++index);
            }    
        }

        private IntPtr FindWindow(string title, int index)
        {
            List<Process> l = new List<Process>();

            Process[] tempProcesses;
            tempProcesses = Process.GetProcesses();
            foreach (Process proc in tempProcesses)
            {
                if (proc.MainWindowTitle == title)
                {
                    l.Add(proc);
                }
            }

            if (l.Count > index) return l[index].MainWindowHandle;
            return (IntPtr)0;
        }

    }
}
