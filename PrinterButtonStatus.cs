using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrinterButton
{
    public enum Command { RedOff, RedOn, GreenOff, GreenOn };

    public static class SerialPortExtension
    {
        public static void SendCommand(this SerialPort port, Command command)
        {
            port.WriteLine(
                    command == Command.GreenOff ? "A" :
                    command == Command.GreenOn ? "B" :
                    command == Command.RedOff ? "C" :
                    "D"
            );
        }
    }

    public partial class PrinterButtonStatus : Form
    {
        PrintDocument document;
        SerialPort port;
        string comPort;
        Thread thread;

        public PrinterButtonStatus(PrintDocument document, string comPort)
        {
            this.document = document;
            this.comPort = comPort;
            this.FormClosed += PrinterButtonStatus_FormClosed;

            InitializeComponent();
            thread = new Thread(AutoprintThread);
            thread.Start();
        }

        private void PrinterButtonStatus_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (port != null)
            {
                port.Close();
                thread.Join();
            }
        }

        void SetStatus(string text)
        {
            if (InvokeRequired)
                Invoke(new System.Action(() => SetStatus(text)));
            else
            {
                lblStatus.Text = text;
            }
        }

        void AutoprintThread()
        {
            try
            {
                SetStatus("Connecting to Arduino...");
                try
                {
                    port = new SerialPort(comPort, 9600);
                    port.Open();
                    if (!port.IsOpen)
                    {
                        throw new Exception("Failed to open COM port?");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    port.Close();
                    port = null;
                    Application.Exit();
                    return;
                }

                SetStatus("Testing Lights..");
                for (int i = 0; i < 3; i++)
                {
                    port.SendCommand(Command.GreenOn);
                    port.SendCommand(Command.RedOn);
                    Thread.Sleep(250);
                    port.SendCommand(Command.GreenOff);
                    port.SendCommand(Command.RedOff);
                    Thread.Sleep(250);
                }

                while (true)
                {
                    SetStatus("Ignoring Previous Input...");
                    while (port.BytesToRead > 0)
                        port.ReadByte();

                    SetStatus("Waiting for Button Press...");
                    port.SendCommand(Command.RedOn);
                    while (true)
                    {
                        string line = port.ReadLine();
                        if (line.Contains("H"))
                        {
                            port.SendCommand(Command.RedOff);
                            while (port.BytesToRead > 0)
                                port.ReadByte();
                            break;
                        }
                    }

                    SetStatus("Printing!");
                    port.SendCommand(Command.GreenOn);
                    Thread.Sleep(250);
                    try
                    {
                        document.Print();

                        System.Printing.LocalPrintServer server = new System.Printing.LocalPrintServer();
                        foreach (PrintQueue queue in server.GetPrintQueues())
                        {
                            while (queue.IsBusy || queue.IsQueued || queue.IsWarmingUp)
                            {
                                SetStatus("Waiting for " + queue.FullName + " " +
                                    (queue.IsBusy ? "Busy" : queue.IsQueued ? "Queued" : queue.IsWarmingUp ? "Warming Up" : ""));
                                Thread.Sleep(250);
                                port.SendCommand(Command.GreenOff);
                                Thread.Sleep(250);
                                port.SendCommand(Command.GreenOn);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + "\n" + e.StackTrace, "Failed to print :(");
                    }

                    SetStatus("Printing Complete!");
                    for (int i = 0; i < 3; i++)
                    {
                        port.SendCommand(Command.GreenOff);
                        port.SendCommand(Command.RedOn);
                        Thread.Sleep(250);
                        port.SendCommand(Command.GreenOn);
                        port.SendCommand(Command.RedOff);
                        Thread.Sleep(250);
                    }
                    port.SendCommand(Command.GreenOff);
                    port.SendCommand(Command.RedOff);
                }
            }
            catch (IOException)
            {
                return;
            }
        }

        void lblPrint_Click(object sender, EventArgs e)
        {
            document.Print();
        }
    }
}
