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
        string fotoPath;
        string txtPath;
        string directory;
        Stream fotoStream;
        string text = "";
        //Creo instancia de cliente API
        ImageAnnotatorClient client = ImageAnnotatorClient.Create();
        public Form1()
        {
            InitializeComponent();
        }

            //Botón examina carpeta que contiene lote de cheques
            private void openButton_Click(object sender, EventArgs e) {
            // Consigo path de imagen 
            fotoStream = null;
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "Archivos jpg (*.jpg)|*.jpg*|Todos los archivos (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                fotoStream = openFileDialog1.OpenFile();
                fotoPath = openFileDialog1.FileName;
                directory = Path.GetDirectoryName(fotoPath);
            }
            textBox1.Text = ("Cargando imagen...");
            //improveImage(fotoStream, directory);
            textBox1.Text = ("Imagen cargada. Listo para procesar.");
            processButton.Enabled = true;
        }

            //Botón procesa lote
            private void processButton_Click(object sender, EventArgs e) {
                textBox1.Text = "Procesando...";

                

                textBox1.Text = "Se creó un txt con el resultado.";
                processButton.Enabled = false;
                
            }
            

            public void processSingleCheck(string img) {
                //Creo archivo txt 
                CreateTXT(directory);

                //Creo rectángulos de recortes que interesan para procesar
                Rectangle z1 = new Rectangle(3, 3, 3, 3);
                Rectangle z2 = new Rectangle(3, 3, 3, 3);
                Rectangle z3 = new Rectangle(3, 3, 3, 3);
                Rectangle[] cropZones = new Rectangle[] { z1, z2, z3 };
                //Proceso cada recorte de imagen original 
                foreach (Rectangle z in cropZones) {
                    //Hago recorte para procesar
                    var imageFactory = new ImageFactory(false);
                    var croppedImg = imageFactory.Load(fotoPath);
                    croppedImg.Crop(z);
                    //Guardo archivo con recorte
                    croppedImg.Save(string.Concat(directory, "\\ManusE1_temporal\\current_crop.tiff"));
                    //Libero memoria de objeto croppedImg, para crear otro en próxima iteración
                    croppedImg.Dispose();

                    //Proceso recorte
                    processSingleCrop("\\ManusE1_temporal\\current_crop.tiff");

                    //Elimino archivo de recorte creado luego de haberlo procesado
                    File.Delete(string.Concat(directory, "\\ManusE1_temporal\\current_crop.tiff"));
                }
            }

            public void processSingleCrop(string img) {
                //Pido a la API Vision
                var image = Google.Cloud.Vision.V1.Image.FromFile(img);
                var response = client.DetectText(image);

                //Escribo lo devuelto por Vision 
                text = response.ElementAt(0).Description;
                AppendText2File(directory, text);
            }

            public void CreateTXT(string directory) {
                txtPath = string.Concat(directory, "\\Texto Resultado.txt"); //Creo el archivo con nombre "Texto Resultado"
                if (File.Exists(txtPath)) {
                    textBox1.Text = "Ya existía otro archivo de texto resultado. Se reemplazó.";
                }
                FileStream file = File.Create(txtPath);
                file.Dispose(); //Elimino el objeto para que writeTXT pueda usar el archivo creado sin que este proceso lo tenga abierto.
            }

            public void AppendText2File(string txtFilePath, string text) {
                StreamWriter file = new StreamWriter(txtFilePath);
                File.AppendAllText(txtFilePath, text + Environment.NewLine);
                //file.Write(text); 
                file.Dispose();
            }
         






            public void improveImage(Stream img, string directory) {
                var imageFactory = new ImageFactory(false);
                var improvedImg = imageFactory.Load(img);
                //Aplico mejoras
                improvedImg.Filter(MatrixFilters.BlackWhite);
                improvedImg.Contrast(70);
                improvedImg.AutoRotate();

                //Guardo imagen mejorada
                improvedImg.Save(string.Concat(directory, "\\improvedImg.jpg"));
            }

        }
    }
