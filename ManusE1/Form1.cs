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
        enum checkType {banco_comercial, bbva};

        //Creo instancia de cliente API
        ImageAnnotatorClient client = ImageAnnotatorClient.Create();
        ImageContext ic = new ImageContext();

        public Form1()
        {
            InitializeComponent();
        }

            //Botón examina carpeta que contiene lote de cheques
            private void openButton_Click(object sender, EventArgs e) {
                // Consigo path directorio de lote cheques
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
                //Seteo español como contexto de la imagen
                ic.LanguageHints.Add("es");
                //Creo array de todos los .tif que existen en la carpeta que contiene imágenes a procesar
                string[] checkFiles = Directory.GetFiles(workingDirectory, "*.*", SearchOption.TopDirectoryOnly);
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
                string bank;
                string resultingTxtDirectory = string.Concat(workingDirectory, "\\Archivos de texto resultado");
                CreateTXT(resultingTxtDirectory, outcomeTxt);

                //Creo array de campos que interesan procesar del cheque 
                string[] cropFields = { "Serie: ", "Número de cheque: ", "Monto: ", "Monto en letras: ", "Lugar de pago: ", 
                                       "Fecha: ", "Beneficiario: ", "Número de cuenta: ", "Titular de la cuenta: ", "Código de cheque: " };

                Rectangle nr, z1, z2, z3, z4, z5, z6, z7, z8, z9, z10;
                nr = new Rectangle(0, 0, 0, 0);
                z1 = z2 = z3 = z4 = z5 = z6 = z7 = z8 = z9 = z10 = nr;
                
                if (detectCheckType(check) == 1) { // Cheque de Banco Comercial
                    bank = "Banco Comercial";
                    AppendText2File(workingDirectory + "\\Archivos de texto resultado\\" + outcomeTxt + ".txt", "Cheque de " + bank);
                    //Creo rectángulos de recortes de los campos a procesar
                    z1 = new Rectangle(50, 80, 60, 50); //zona de serie
                    z2 = new Rectangle(140, 80, 140, 50); //zona de número de cheque
                    z3 = new Rectangle(1040, 10, 310, 100); //zona de monto
                    z4 = new Rectangle(195, 240, 1170, 70); //zona de monto en letras
                    z5 = new Rectangle(180, 150, 710, 30); //zona de lugar de pago
                    z6 = new Rectangle(680, 165, 690, 45); //zona de fecha
                    z7 = new Rectangle(195, 180, 500, 70); //zona de beneficiario
                    z8 = new Rectangle(50, 380, 250, 45); //zona de número cuenta
                    z9 = new Rectangle(50, 425, 450, 45); //zona de titular de la cuenta
                    z10 = new Rectangle(70, 490, 810, 110); //zona de código de cheque
                }
                else if (detectCheckType(check) == 2) { // Cheque de BBVA 
                    bank = "BBVA";
                    AppendText2File(workingDirectory + "\\Archivos de texto resultado\\" + outcomeTxt + ".txt", "Cheque de " + bank);
                    z1 = new Rectangle(50, 20, 28, 18); //zona de serie
                    z2 = new Rectangle(80, 20, 65, 23); //zona de número de cheque
                    z3 = new Rectangle(480, 15, 215, 36); //zona de monto
                    z4 = new Rectangle(130, 115, 555, 34); //zona de monto en letras
                    z5 = new Rectangle(140, 45, 410, 21); //zona de lugar de pago
                    z6 = new Rectangle(540, 65, 150, 28); //zona de fecha
                    z7 = new Rectangle(140, 90, 427, 25); //zona de beneficiario
                    z8 = new Rectangle(30, 185, 177, 22); //zona de número cuenta
                    z9 = new Rectangle(30, 202, 268, 22); //zona de titular de la cuenta
                    z10 = new Rectangle(35, 250, 405, 47); //zona de código de cheque
                }

                Rectangle[] cropZones = new Rectangle[] { z1, z2, z3, z4, z5, z6, z7, z8, z9, z10 };
                //Proceso cada recorte
                int cropNumber = 0;
                foreach (Rectangle z in cropZones) {
                    //Hago recorte para procesar
                    var imageFactory = new ImageFactory(false);
                    var croppedImg = imageFactory.Load(check);
                    croppedImg.Crop(z);
                    //Guardo archivo con recorte en formanto png
                    croppedImg.Format(new PngFormat { Quality = 100 });
                    croppedImg.Save(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.png"));

                    //Proceso recorte
                    processSingleCrop(workingDirectory + "\\ManusE1_temporal\\current_crop.png", workingDirectory + "\\Archivos de texto resultado\\" + outcomeTxt +".txt", cropFields[cropNumber]);
                    cropNumber += 1;

                    //Elimino recorte creado luego de haberlo procesado
                    croppedImg.Dispose();
                    File.Delete(string.Concat(workingDirectory, "\\ManusE1_temporal\\current_crop.png"));
                }
            }

            public int detectCheckType(string check) { // Código 1: Banco Comercial, Código 2: BBVA
                System.Drawing.Image img = System.Drawing.Image.FromFile(check);
                //Recorto logo banco
                var imageFactory = new ImageFactory(false);
                var croppedImg = imageFactory.Load(check);
                int x, y, width, height;
                x = Convert.ToInt32(img.Width * 0.33);
                y = 0;
                width = Convert.ToInt32(img.Width * 0.33);
                height = Convert.ToInt32(img.Height * 0.25);
                Rectangle logoZone = new Rectangle(x, y, width, height);
                croppedImg.Crop(logoZone);
                
                //Guardo archivo con recorte en formanto png
                croppedImg.Format(new PngFormat { Quality = 100 });
                string logoImg = string.Concat(workingDirectory, "\\ManusE1_temporal\\logo.png");
                croppedImg.Save(logoImg);

                //Proceso logo
                try {
                    //Evalúo lo devuelto por Vision
                    var image = Google.Cloud.Vision.V1.Image.FromFile(logoImg);
                    var response = client.DetectText(image, ic);
                    string OCRtext = response.ElementAt(0).Description;

                    //Elimino recorte creado luego de haberlo procesado
                    croppedImg.Dispose();
                    File.Delete(string.Concat(workingDirectory, "\\ManusE1_temporal\\logo.png"));

                    if (OCRtext.Contains("MER")) {
                        return 1;
                    } else if (OCRtext.Contains("BBVA")) {
                        return 2;
                    }
                }
                catch (ArgumentOutOfRangeException) {
                    //Si ElementAt(0) = null porque Vision no devolvió texto, asumo que es cheque de Banco Comercial por mayor probabilidad
                    return 1;
                }
                return 0; //Nunca devolverá este valor, pero el compilador lo demandaba
            }

            public void processSingleCrop(string cropImg, string txtFile, string cropField) {
                //Pido a la API Vision
                var image = Google.Cloud.Vision.V1.Image.FromFile(cropImg);
                var response = client.DetectText(image, ic);
                try { 
                    //Escribo lo devuelto por Vision 
                    string OCRtext = response.ElementAt(0).Description;
                    AppendText2File(txtFile, cropField + OCRtext);
                }
                //Si ElementAt(0) = null porque Vision no devolvió texto
                catch (ArgumentOutOfRangeException) {
                    AppendText2File(txtFile, cropField + "No se pudo leer el campo o está vacío");
                }
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
