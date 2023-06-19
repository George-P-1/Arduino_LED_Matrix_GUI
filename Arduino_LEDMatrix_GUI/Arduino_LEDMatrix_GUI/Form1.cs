using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Arduino_LEDMatrix_GUI
{
    public partial class Form1 : Form
    {
        // For GUI
        private PictureBox[] pictureBoxes;
        private bool[] ledStates = new bool[9]; // Initializes with false.

        // For Serial Port Comm - Transmit
        private String[] ports;
        private bool isConnected = false;
        private String message4transmit;

        // For Serial Port - Receive
        public delegate void dele(string indata); // indata is input data
        private String lightingState;
        private String presentationState;

        /// <summary>
        /// Initialize the Form/Program with default GUI and Serial Comm settings.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.icon_32;

            // Initilize Picture Boxes as all off
            InitializePictureBoxes();

            // Serial Port Stuff
            disableControls();
            getAvailableComPorts();
        }

        /// <summary>
        /// Initialize PictureBox objects into a list and LED states set to OFF.
        /// </summary>
        private void InitializePictureBoxes()
        {
            pictureBoxes = new PictureBox[]
            {
            pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
            pictureBox6, pictureBox7, pictureBox8, pictureBox9
            };

            for (int i = 0; i < pictureBoxes.Length; i++)
            {
                UpdateLedState(pictureBoxes[i], ledStates[i]);
            }
        }

        /// <summary>
        /// Update LED State of PictureBox object <paramref name="pb"/> according to <paramref name="ledState"/>
        /// </summary>
        /// <param name="pb"></param>
        /// <param name="ledState"></param>
        private void UpdateLedState(PictureBox pb, bool ledState)
        {
            if (ledState)
            {
                pb.Image = Properties.Resources.ON_LED_50; // Set the image for LED ON
            }
            else
            {
                pb.Image = Properties.Resources.OFF_LED_50; // Set the image for LED OFF
            }
        }

        /// <summary>
        /// When an LED is clicked or double-clicked, toggle its current state.
        /// </summary>
        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBox clickedPictureBox = (PictureBox)sender;
            int index = Array.IndexOf(pictureBoxes, clickedPictureBox);
            ledStates[index] = !ledStates[index];
            UpdateLedState(clickedPictureBox, ledStates[index]);
        }

        /// <summary>
        ///  Gets all currently available ports and adds them to drop-down list.
        /// </summary>
        void getAvailableComPorts()
        {
            ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear(); // First Remove all current options in drop-down list.
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
                if (ports[0] != null) // Select First by default
                {
                    comboBox1.SelectedItem = ports[0];
                }
            }
        }

        /// <summary>
        /// Scan for currently available ports. 
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            getAvailableComPorts();
        }

        /// <summary>
        /// Connect or Disconnect to a selected COM Port
        /// </summary>
        /// <param name="sender"> Clicked Object </param>
        /// <param name="e"> Event Data </param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                connectToArduino();
            }
            else
            {
                disconnectFromArduino();
            }
        }

        /// <summary>
        /// Connect to selected COM Port.
        /// </summary>
        private void connectToArduino()
        {
            isConnected = true;
            string selectedPort = comboBox1.GetItemText(comboBox1.SelectedItem);
            serialPort1.PortName = selectedPort;
            serialPort1.Open();
            serialPort1.Write("#STAR\r\n"); // For debugging
            button1.Text = "Disconnect";
            enableControls();
            button2.Enabled = false;
        }

        /// <summary>
        /// Disconnect COM Port.
        /// </summary>
        private void disconnectFromArduino()
        {
            isConnected = false;
            serialPort1.Write("#STOP\r\n"); // For debugging
            serialPort1.Close();
            button1.Text = "Connect";
            disableControls();
            button2.Enabled = true;
            // Reset Status
            textBox1.Text = "No Data";
            textBox1.ForeColor = Color.Black;
            textBox2.Text = "No Data";
            textBox2.ForeColor = Color.Black;
        }

        /// <summary>
        /// Disable the ability to change LED states in GUI and send data to COM port.
        /// </summary>
        private void disableControls()
        {
            for (int i = 0; i < pictureBoxes.Length; i++)
            {
                ledStates[i] = false;
                UpdateLedState(pictureBoxes[i], ledStates[i]);
            }
            button3.Enabled = false;
            groupBox1.Enabled = false;
        }

        /// <summary>
        /// Enable the ability to change LED states and send data to COM port.
        /// </summary>
        private void enableControls()
        {
            foreach (PictureBox pb in pictureBoxes)
            {
                pb.Enabled = true;
            }
            button3.Enabled = true;
            groupBox1.Enabled = true;
        }

        /// <summary>
        /// Sends current LED Matrix pattern info to arduino.
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            // Generate Sequence from ledStates list into message4transmit
            message4transmit = "#C";
            for (int i = 0; i < ledStates.Length; i++)
            {
                message4transmit += ledStates[i] ? '1' : '0';
            }
            message4transmit += "\r\n";
            Console.WriteLine(message4transmit); // Writes to windows Console
            // Send sequence to COM
            serialPort1.Write(message4transmit);
        }

        /// <summary>
        /// Receives/reads information from the Serial Port and calls a delegate
        /// </summary>
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    string indata = serialPort1.ReadLine(); // Read Serial Input
                    dele writeit = new dele(Write2GUI);
                    Invoke(writeit, indata);
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Error opening serial port: " + ex.Message);
            }
        }

        /// <summary>
        /// Handles data sent from Serial Port (<paramref name="indata"/>) and updates GUI
        /// </summary>
        /// <param name="indata">Input string from Serial port</param>
        public void Write2GUI(string indata)
        {
            Console.WriteLine(indata);
            if (true /*indata.Length == 5*/)
            {
                if (indata[0] == '*' && indata[1] == 'M')
                {
                    switch (indata[2])
                    {
                        case '1':
                            lightingState = "ALL ON";
                            textBox1.ForeColor = Color.Green;
                            presentationState = "ON";
                            textBox2.Text = presentationState;
                            break;
                        case '2':
                            lightingState = "ALL OFF";
                            textBox1.ForeColor = Color.Red;
                            presentationState = "OFF";
                            textBox2.Text = presentationState;
                            break;
                        case '3':
                            lightingState = "AUTO";
                            textBox1.ForeColor = Color.Blue;
                            break;
                        case '4':
                            lightingState = "Custom";
                            textBox1.ForeColor = Color.Purple;
                            break;
                        default:
                            /*lightingState = "NO DATA";
                            textBox1.ForeColor = Color.Black;*/
                            break;
                    }
                    textBox1.Text = lightingState;
                }
                if (indata[0] == '*' && indata[1] == 'P')
                {
                    if (indata[2] == '1')
                    {
                        presentationState = "ON";
                    }
                    else
                    {
                        presentationState = "OFF";
                    }
                    textBox2.Text = presentationState;
                }
            }
        }
    }
}
