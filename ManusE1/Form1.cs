using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Google.Cloud.Vision.V1;
using ImageProcessor;
using ImageProcessor.Imaging.Filters.Photo;
using System.Drawing;
using ImageProcessor.Imaging.Formats;

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
                // Consigo path de imagen 
                folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
                    workingDirectory = folderBrowserDialog1.SelectedPath; 
                }
                //Elimino carpeta temporal que el programa utiliza y de txt resultados, en caso de que ya existieran por algún motivo
                cleanEnvironment(true);

                textBox1.Text = ("Listo para procesar.");
                //improveImage(fotoStream, directory);
                processButton.Enabled = true;
        }

            //Botón procesa lote
            private void processButton_Click(object sender, EventArgs e) {
                //Creo array de todos los .tif que existen en la carpeta que contiene imágenes a procesar
                string[] checkFiles = Directory.GetFiles(workingDirectory, "*.tif", SearchOption.TopDirectoryOnly);
                int totalFiles = checkFiles.Length;
                if (totalFiles == 0) {
                    textBox1.Text = "La carpeta seleccionada no contiene cheques.";
                    processButton.Enabled = false;
                } else {
                    //Proceso lote de cheques
                    processButton.Enabled = false;
                    openButton.Enabled = false;
                    int currentFile = 0;
                    Directory.CreateDirectory(workingDirectory + "\\Archivos de texto resultado");
                    foreach (string check in checkFiles) {
                        currentFile += 1;
                        processSingleCheck(check, "Cheque " + currentFile);
                        textBox1.Text = "Procesando cheque " + currentFile + " de " + totalFiles + ".";
                    }
                    textBox1.Text = "Lote de cheques procesado.";
                    openButton.Enabled = true;
                }
                //Elimino carpeta temporal que el programa utiliza
                cleanEnvironment(false);
            }
            

            public void processSingleCheck(string check, string outcomeTxt) {
                //Creo archivo txt 
                string resultingTxtDirectory = string.Concat(workingDirectory, "\\Archivos de texto resultado");
                CreateTXT(resultingTxtDirectory, outcomeTxt);

                //Creo array de campos que interesan procesar del cheque
                string[] cropFields = { "Número de cheque: ", "Monto: ", "Lugar de pago: " };
                //Creo rectángulos de recortes de los campos a procesar
                Rectangle z1 = new Rectangle(40, 140, 200, 60); //zona de número de cheque
                Rectangle z2 = new Rectangle(1040, 10, 310, 100); //zona de monto
                Rectangle z3 = new Rectangle(180, 150, 710, 30); //zona de lugar de pago
                Rectangle[] cropZones = new Rectangle[] { z1, z2, z3 };
                //Proceso cada recorte
                var imageFactory = new ImageFactory(false);
                var croppedImg = imageFactory.Load(check);
                int cropNumber = 0;
                foreach (Rectangle z in cropZones) {
                    //Hago recorte para procesar
                    croppedImg.Crop(z);
                    //Guardo archivo con recorte en formanto png
                    croppedImg.Format(new PngFormat { Quality = 10 });
                    croppedImg.Save(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.png"));

                    //Proceso recorte
                    processSingleCrop(workingDirectory + "\\ManusE1_temporal\\current_crop.png", workingDirectory + "\\Archivos de texto resultado\\" + outcomeTxt +".txt", cropFields[cropNumber]);
                    cropNumber += 1;

                    //Elimino archivo de recorte creado luego de haberlo procesado
                    File.Delete(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.png"));
                }
                croppedImg.Dispose();
            }

            public void processSingleCrop(string cropImg, string txtFile, string cropField) {
                //Pido a la API Vision
                var image = Google.Cloud.Vision.V1.Image.FromFile(cropImg);
                var response = client.DetectText(image);

                //Escribo lo devuelto por Vision 
                string OCRtext = response.ElementAt(0).Description;
                AppendText2File(txtFile, cropField + OCRtext);
            }

            public void CreateTXT(string directory, string fileName) {
                string txtPath = directory + "\\" + fileName + ".txt";
                //Creo el archivo txt
                FileStream file = File.Create(txtPath);
                file.Dispose(); //Elimino el objeto para que AppendText2File pueda usar el archivo creado sin que este proceso lo tenga abierto.
            }

            public void AppendText2File(string txtFilePath, string text) {
                StreamWriter file = File.AppendText(txtFilePath);
                file.WriteLine(text);
                file.Dispose();
            }

            public void cleanEnvironment(bool full) {
                string manusTemporalFolder = string.Concat(workingDirectory, "\\ManusE1_temporal");
                if (Directory.Exists(manusTemporalFolder)) {
                    Directory.Delete(manusTemporalFolder, true);
                }
                if (full == true) {
                    if (Directory.Exists(workingDirectory + "\\Archivos de texto resultado")) {
                        Directory.Delete(workingDirectory + "\\Archivos de texto resultado", true);
                    }
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
