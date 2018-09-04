using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.IO;
using OCR_Prototype.Models;
using System.Drawing;
using System.Drawing.Imaging;
using IronOcr;
using PdfToImage;
using System.Text.RegularExpressions;
using static OCR_Prototype.Models.OCRModel;
using System.Diagnostics;
using System.Configuration;

namespace OCR_Prototype.Controllers
{
    public class OCRController : Controller
    {
        // GET: OCR
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Upload()
        {
            OCRModel DDLFormList = new OCRModel();

            return View(DDLFormList.GetDDLForm());
        }

        public ActionResult FormListing()
        {
            OCRModel ListResult = new OCRModel();

            return View(ListResult.getListing());
        }

        public ActionResult FormDetail(string id)
        {
            OCRModel getDetailResult = new OCRModel();

            return View(getDetailResult.getDetailList(id));
        }

        public ActionResult AccOpen()
        {
            return View();
        }

        public ActionResult AccOpen2(string item_id)
        {
            OCRModel getDetailResult = new OCRModel();

            return View(getDetailResult.getDetailList(item_id));
        }

        public ActionResult InHouseApp(string item_id)
        {
            OCRModel getDetailResult = new OCRModel();

            return View(getDetailResult.getDetailList(item_id));
        }

        [HttpPost]
        public ActionResult UploadImg(int DocID)
        {
            string path = "";
            string fileName = "";
            string convertpath = "";
            string filenamenoext = "";
            string fileext = "";
            string relativepath = "";
            List<int> ResultFormID = new List<int>();
            //int DocID = 0;
            var docpage = new List<InsertformInfo>();
            int totalpageno = 0;

            //DocID = Convert.ToInt32(Request.Form["DDlDoc"].ToString());

            OCRModel imgpath = new OCRModel();
            try
            {
                if (Request.Files.Count > 0)
                {
                    var file = Request.Files[0];

                    if (file != null && file.ContentLength > 0)
                    {
                        filenamenoext = Path.GetFileNameWithoutExtension(file.FileName);
                        fileName = Path.GetFileName(file.FileName);
                        fileext = Path.GetExtension(file.FileName);
                        path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);

                        //Save File to destination folder
                        file.SaveAs(path);

                        if (fileext == ".pdf")
                        {
                            //Get pdf total page number
                            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                            StreamReader r = new StreamReader(fs);
                            string pdfText = r.ReadToEnd();
                            Regex rx1 = new Regex(@"/Type\s*/Page[^s]");
                            MatchCollection matches = rx1.Matches(pdfText);
                            totalpageno = matches.Count;

                            for (int i = 1; i <= totalpageno; i++)
                            {
                                PdfToImage.PDFConvert pp = new PDFConvert();
                                pp.OutputFormat = "jpeg"; //format
                                pp.JPEGQuality = 100; //100% quality
                                pp.ResolutionX = 300; //dpi
                                pp.ResolutionY = 300;
                                pp.FirstPageToConvert = i; //pages you want
                                pp.LastPageToConvert = totalpageno;
                                convertpath = Path.Combine(Server.MapPath("~/Content/Images/"), filenamenoext + "_pg" + i + ".jpeg");
                                relativepath = "~/Content/Images/" + filenamenoext + "_pg" + i + ".jpeg";
                                pp.Convert(path, convertpath);

                                docpage.Add(new InsertformInfo
                                {
                                    pageNo = i,
                                    docpathref = relativepath,
                                    physicalpath = convertpath
                                });
                            }
                        }
                        else
                        {
                            convertpath = path;
                            relativepath = "~/Content/Images/" + fileName;

                            //Shaun : Non Pdf file 
                            docpage.Add(new InsertformInfo
                            {
                                pageNo = 1,
                                docpathref = relativepath,
                                physicalpath = convertpath
                            });
                        }
                        writeLog(DateTime.Now.ToString() + "Able to save the orignal Image");
                        //Shaun : Save sourcefile information to DB
                        ResultFormID = imgpath.insertForm(docpage, DocID);
                    }
                }

                //Shaun : Get position for each box position to crop
                if (ResultFormID.Count != 0)
                {
                    OCRModel obj = new OCRModel();
                    var PosResult = new List<OCRModel.Position>();

                    //Get position for each box
                    PosResult = imgpath.retrieveBoxPos(DocID);

                    if (totalpageno == 0)
                    {
                        //writeLog(DateTime.Now.ToString() + "Convert Single Page");
                        int passID = ResultFormID[0];
                        cropImage_Convert(PosResult, path, filenamenoext, passID);
                    }
                    else
                    {
                        //writeLog(DateTime.Now.ToString() + "Convert Multi Page");
                        //Crop Image base on position and convert to image
                        cropImage_ConvertMulti(PosResult, docpage, filenamenoext, ResultFormID, totalpageno);
                    }
                }
                else
                {
                    //Shaun : Exception Handling [TBC]
                }
            }
            catch(Exception ex)
            {
                writeLog(ex.ToString());
            }
            //return RedirectToAction("Upload");
            return PartialView("_SystemMessage");

        }

        //Shaun : Crop Image and Convert to text for Multipage [currently for pdf format only]
        public void cropImage_ConvertMulti(List<OCRModel.Position> CropPos, List<InsertformInfo> imagePath, string name, List<int> formID, int totalpage)
        {
            OCRModel imgpath = new OCRModel();
            OCRModel obj = new OCRModel();
            var CropRes = new List<OCRModel.CropResult>();
            int i = 0;
            string croppath = "";
            string relativecrop = "";
            Bitmap croppedImage;

            try
            {
                for (int j = 1; j <= totalpage; j++)
                {
                    i = 0;

                    for (i = 0; i < CropPos.Count; i++)
                    {
                        if (j == CropPos[i].page)
                        {
                            j--;
                            // Here we capture the resource - image file.
                            using (var originalImage = new Bitmap(imagePath[j].physicalpath))
                            {
                                //Set Position {x1,y1,width,height}
                                Rectangle crop = new Rectangle(CropPos[i].pos_X1, CropPos[i].pos_Y1, CropPos[i].pos_width, CropPos[i].pos_height);

                                // Here we capture another resource.
                                croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);

                            }// Here we release the original resource - bitmap in memory and file on disk.

                            // At this point the file on disk already free - you can record to the same path.
                            //croppedImage.Save(@"C:\Users\kazarboys\Source\Repos\How-to-use-tesseract-ocr-4.0-with-csharp\tesseract-master.1153\samples\crop.jpg", ImageFormat.Jpeg);
                            croppath = Path.Combine(Server.MapPath("~/Content/Images/crop"), name + "" + formID[j] + "_crop_" + i + ".jpg");
                            relativecrop = "~/Content/Images/crop/" + name + "" + formID[j] + "_crop_" + i + ".jpg";
                            croppedImage.Save(croppath, ImageFormat.Jpeg);

                            /*var Ocr = new AutoOcr();
                            var OcrResult = Ocr.Read(croppath);
                            Console.WriteLine(OcrResult.Text);*/

                            IronOcr.License.LicenseKey = "IRONOCR-12906173F4-554056-9099E8-9D2F53E456-3B543C5-UExE92AE3811C5F5D8-TRIAL.EXPIRES.30.JUL.2018";
                            //bool result = IronOcr.License.IsValidLicense("IRONOCR-12906173F4-554056-9099E8-9D2F53E456-3B543C5-UExE92AE3811C5F5D8-TRIAL.EXPIRES.30.JUL.2018");
                            //writeLog(DateTime.Now.ToString() + "Check Licence : " + result);

                            var Ocr = new AdvancedOcr()
                            {
                                //Language = IronOcr.Languages.Japanese.OcrLanguagePack,
                                CleanBackgroundNoise = false,
                                ColorDepth = 2,
                                ColorSpace = AdvancedOcr.OcrColorSpace.Color,
                                EnhanceContrast = false,
                                DetectWhiteTextOnDarkBackgrounds = false,
                                RotateAndStraighten = false,
                                Strategy = AdvancedOcr.OcrStrategy.Advanced
                            };
                            var OcrResult = Ocr.Read(croppath);
                            //Console.WriteLine(OcrResult.Text);

                            //var Ocr = new AutoOcr();
                            //var OcrResult = Ocr.Read(croppath);

                            writeLog(DateTime.Now.ToString() + "Result : " + OcrResult.Text);

                            CropRes.Add(new OCRModel.CropResult
                            {
                                FormID_Key = formID[j],
                                Crop_Imgpath = relativecrop.ToString(),
                                Crop_Text = OcrResult.ToString().Replace("\r\n", "\\r\\n")
                            });

                            // It is desirable release this resource too.
                            croppedImage.Dispose();
                            j++;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                writeLog(ex.ToString());
            }

            //Insert CropImage to DB
            imgpath.InsertCropResult(CropRes);
        }

        //Shaun : Crop Image and Convert to text for single page [currently for jpeg format only]
        public void cropImage_Convert(List<OCRModel.Position> CropPos, string imagePath, string name, int formID)
        {
            OCRModel imgpath = new OCRModel();
            OCRModel obj = new OCRModel();
            var CropRes = new List<OCRModel.CropResult>();

            string croppath = "";
            string relativecrop = "";
            Bitmap croppedImage;
            writeLog(DateTime.Now.ToString() + "Single page function");
            try
            {
                for (int i = 0; i < CropPos.Count; i++)
                {
                    //writeLog(DateTime.Now.ToString() + "Crop Begin");
                    // Here we capture the resource - image file.
                    using (var originalImage = new Bitmap(imagePath))
                    {
                        //Set Position {x1,y1,width,height}
                        Rectangle crop = new Rectangle(CropPos[i].pos_X1, CropPos[i].pos_Y1, CropPos[i].pos_width, CropPos[i].pos_height);

                        // Here we capture another resource.
                        croppedImage = originalImage.Clone(crop, originalImage.PixelFormat);

                    }// Here we release the original resource - bitmap in memory and file on disk.

                    // At this point the file on disk already free - you can record to the same path.

                    croppath = Path.Combine(Server.MapPath("~/Content/Images/crop"), name + "" + formID + "_crop_" + i + ".jpg");
                    relativecrop = "~/Content/Images/crop/" + name + "" + formID + "_crop_" + i + ".jpg";
                    croppedImage.Save(croppath, ImageFormat.Jpeg);

                    //writeLog(DateTime.Now.ToString() + "Crop End and OCR BEGIN");
                    //IronOcrInstallation.InstallationPath = "E:\\New folder";
                    //string temp = IronOcrInstallation.InstallationPath.ToString();
                    //writeLog(DateTime.Now.ToString() + "OCR TEMP FOlder : " + temp);
                    //writeLog(DateTime.Now.ToString() + "TEMP FOlder : " + System.IO.Path.GetTempPath());
                    //writeLog(DateTime.Now.ToString() + "Crop Path : " + croppath);

                    IronOcr.License.LicenseKey = "IRONOCR-1387762394-808609-BEB523-D6C18404E2-47CC264-UEx0DCC6578538F5D8-TRIAL.EXPIRES.02.AUG.2018";
                    bool result = IronOcr.License.IsValidLicense("IRONOCR-1387762394-808609-BEB523-D6C18404E2-47CC264-UEx0DCC6578538F5D8-TRIAL.EXPIRES.02.AUG.2018");
                    writeLog(DateTime.Now.ToString() + "Check Licence : " + result);

                    var Ocr = new AdvancedOcr()
                    {
                        CleanBackgroundNoise = false,
                        ColorDepth = 4,
                        ColorSpace = AdvancedOcr.OcrColorSpace.Color,
                        EnhanceContrast = false,
                        DetectWhiteTextOnDarkBackgrounds = false,
                        RotateAndStraighten = false,
                        Strategy = AdvancedOcr.OcrStrategy.Advanced
                    };

                    writeLog(DateTime.Now.ToString() + "Crop Path : " + croppath.ToString());
                    var OcrResult = Ocr.Read(croppath);
                    Console.WriteLine(OcrResult.Text);

                    writeLog(DateTime.Now.ToString() + "Result : " + OcrResult.Text);
                    writeLog(DateTime.Now.ToString() + "OCR END");

                    //string Crop_Text = OcrResult.Text.Replace("\r\n", "\\r\\n"); 

                    CropRes.Add(new OCRModel.CropResult
                    {
                        FormID_Key = formID,
                        Crop_Imgpath = relativecrop.ToString(),
                        Crop_Text = OcrResult.ToString().Replace("\r\n", "\\r\\n")
                    });

                    // It is desirable release this resource too.
                    croppedImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                writeLog(ex.ToString());
            }
            imgpath.InsertCropResult(CropRes);
        }

        //Chai: added to get upload images
        public ActionResult OriginalFile(string item_id)
        {
            OCRModel getDetailResult = new OCRModel();

            return View(getDetailResult.getOriFile(item_id));

        }

        //Shaun : Save Updated text from form detail
        public ActionResult SaveDetail(List<string> TextCrop, List<string> FormCropID, string refer)
        {
            OCRModel UpdateInfoControl = new OCRModel();

            UpdateInfoControl.UpdateDetailInfoModel(TextCrop, FormCropID);

            return RedirectToAction("FormDetail/"+refer);
        }

        public void writeLog(string text)
        {
             using (StreamWriter writer = new StreamWriter(ConfigurationManager.AppSettings["Log"], true))
             {
                writer.WriteLine(text.ToString());
             }
        }

    }
}