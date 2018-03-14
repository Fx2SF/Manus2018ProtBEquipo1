using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Google.Cloud.Vision.V1;
using ImageProcessor;
using ImageProcessor.Imaging.Filters.Photo;
using System.Drawing;
namespace ManusE1
{
    public partial class Form1 : Form
    {
        string workingDirectory;

        //Creo instancia de cliente API
        ImageAnnotatorClient client = ImageAnnotatorClient.Create();
        public Form1()
        {
            InitializeComponent();
        }

            //Botón examina carpeta que contiene lote de cheques
            private void openButton_Click(object sender, EventArgs e) {
                //Elimino carpeta temporal que el programa utiliza, en caso de que ya exista por algún motivo
                cleanEnvironment();
                
                // Consigo path de imagen 
                folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
                    workingDirectory = folderBrowserDialog1.SelectedPath; 
                }
                textBox1.Text = ("Listo para procesar.");
                //improveImage(fotoStream, directory);
                processButton.Enabled = true;
        }

            //Botón procesa lote
            private void processButton_Click(object sender, EventArgs e) {
                textBox1.Text = "Comenzando proceso";
                processButton.Enabled = false;
                //Creo array de todos los .tiff que existen en la carpeta que contiene imágenes a procesar
                string[] checkFiles = Directory.GetFiles(workingDirectory, "*.tiff", SearchOption.TopDirectoryOnly);
                int totalFiles = checkFiles.Length;
                int currentFile = 0;
                foreach (string check in checkFiles) {
                    currentFile += 1;
                    processSingleCheck(check, "Cheque" + currentFile);
                    textBox1.Text = "Procesando cheque " + currentFile + " de " + totalFiles;
                }
                textBox1.Text = "Lote de cheques procesado.";
            }
            

            public void processSingleCheck(string check, string outcomeTxt) {
                //Creo archivo txt 
                string resultingTxts = string.Concat(workingDirectory, "\\Archivos de texto resultado.");
                CreateTXT(resultingTxts, outcomeTxt);

                //Creo rectángulos de recortes que interesan para procesar
                Rectangle z1 = new Rectangle(3, 3, 3, 3);
                Rectangle z2 = new Rectangle(3, 3, 3, 3);
                Rectangle z3 = new Rectangle(3, 3, 3, 3);
                Rectangle[] cropZones = new Rectangle[] { z1, z2, z3 };
                //Proceso cada recorte de imagen original 
                var imageFactory = new ImageFactory(false);
                var croppedImg = imageFactory.Load(check);
                foreach (Rectangle z in cropZones) {
                    //Hago recorte para procesar
                    croppedImg.Crop(z);
                    //Guardo archivo con recorte
                    croppedImg.Save(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.tiff"));

                    //Proceso recorte
                    processSingleCrop("\\ManusE1_temporal\\current_crop.tiff");

                    //Elimino archivo de recorte creado luego de haberlo procesado
                    File.Delete(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.tiff"));
                }
                croppedImg.Dispose();
            }

            public void processSingleCrop(string img) {
                //Pido a la API Vision
                var image = Google.Cloud.Vision.V1.Image.FromFile(img);
                var response = client.DetectText(image);

                //Escribo lo devuelto por Vision 
                string text = "";
                text = response.ElementAt(0).Description;
                AppendText2File(workingDirectory, text);
            }

            public void CreateTXT(string directory, string fileName) {
                string txtPath = string.Concat(workingDirectory, fileName); //Creo el archivo con nombre "Texto Resultado"
                FileStream file = File.Create(txtPath);
                file.Dispose(); //Elimino el objeto para que writeTXT pueda usar el archivo creado sin que este proceso lo tenga abierto.
            }

            public void AppendText2File(string txtFilePath, string text) {
                StreamWriter file = new StreamWriter(txtFilePath);
                File.AppendAllText(txtFilePath, text + Environment.NewLine);
                //file.Write(text); 
                file.Dispose();
            }

            public void cleanEnvironment() {
                string manusTemporalFolder = string.Concat(workingDirectory, "\\ManusE1_temporal");
                if (Directory.Exists(manusTemporalFolder)) {
                    Directory.Delete(manusTemporalFolder, true);
                }
            }
               
                
            





            public void improveImage(Stream img, string directory) {
                var imageFactory = new ImageFactory(false);
                var improvedImg = imageFactory.Load(img);
                //Aplico mejoras
                improvedImg.Filter(MatrixFilters.BlackWhite);
                improvedImg.Contrast(70);
                improvedImg.AutoRotate();

                //Guardo imagen mejorada
                improvedImg.Save(string.Concat(workingDirectory, "\\improvedImg.jpg"));
            }

        }
    }
