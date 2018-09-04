using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace OCR_Prototype.Models
{
    public class OCRModel
    {
        public class dropdownForm
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        public class Position
        {
            public int page { get; set; }
            public int pos_X1 { get; set; }
            public int pos_Y1 { get; set; }
            public int pos_width { get; set; }
            public int pos_height { get; set; }
        }

        public class CropResult
        {
            public int FormID_Key { get; set; }
            public string Crop_Imgpath { get; set; }
            public string Crop_Text { get; set; }
        }

        public class Listing
        {
            public int ID { get; set; }
            public string Reference { get; set; }
            public DateTime CreationDate { get; set; }
        }

        public class getDetails
        {
            public int CropID { get; set; }
            public int FormID { get; set; }
            public string Reference { get; set; }
            public string Form_Path { get; set; }
            public string Crop_Imgpath { get; set; }
            public string Crop_Text { get; set; }
            public int pageno { get; set; }
            public int formid { get; set; }
        }

        public class InsertformInfo
        {
            public int pageNo { get; set; }
            public string docpathref { get; set; }
            public string physicalpath { get; set; }
        }

        private SqlConnection con;

        private void connection()
        {
            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);
        }

        //Shaun : retrieve sequence number for form ID
        public string getseqnum()
        {
            string sql = null;
            string sqlseq = null;
            int seqnum = 0;
            string newnum = null;
            string retseq = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlseq = "SELECT VALUE FROM FORM_COMMON WHERE REFERENCE ='Ref_Seq'";

                    using (SqlCommand cmd = new SqlCommand(sqlseq, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            seqnum = Convert.ToInt32(reader.GetString(0));
                            retseq = seqnum.ToString("0000");
                            seqnum += 1;
                            newnum = seqnum.ToString("0000");
                        }
                        reader.Close();
                    }

                    sql = "UPDATE FORM_COMMON set VALUE = '" + newnum + "' WHERE REFERENCE = 'Ref_Seq'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@val", "OI-" + newnum);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
            return retseq;
        }

        //Shaun : save new form
        public List<int> insertForm(List<InsertformInfo> formpath, int formID)
        {
            DateTime date = DateTime.Now;
            string sql = null;
            string seq = getseqnum();

            List<int> getid = new List<int>();

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();

                    for (int i = 0; i < formpath.Count; i++)
                    {

                        sql = "INSERT INTO Form_Image VALUES (@ImgId,@ImgRef,@Imgpath,@Cre_Date,@Cre_By,@pageno); SELECT SCOPE_IDENTITY()";


                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ImgId", formID);
                            cmd.Parameters.AddWithValue("@ImgRef", "OI-" + seq);
                            cmd.Parameters.AddWithValue("@Imgpath", formpath[i].docpathref);
                            cmd.Parameters.AddWithValue("@Cre_Date", date);
                            cmd.Parameters.AddWithValue("@Cre_By", "SCRIPT");
                            cmd.Parameters.AddWithValue("@pageno", formpath[i].pageNo);
                            //cmd.ExecuteNonQuery();

                            getid.Add(Convert.ToInt32(cmd.ExecuteScalar()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
            return getid;
        }

        //Shaun : retrieve box position fo crop
        public List<Position> retrieveBoxPos(int DocID)
        {
            var BoxID = new List<Position>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT A.[Page_No],B.[pos_x1],B.[pos_y1],B.[pos_x2],B.[pos_y2] FROM [Form_Config] A INNER JOIN [FormBox_Position] B ON A.[BoxPosition_ID] = B.[ID] AND A.[Doc_Key] = '" + DocID + "'";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            BoxID.Add(new Position
                            {
                                page = reader.GetInt32(0),
                                pos_X1 = reader.GetInt32(1),
                                pos_Y1 = reader.GetInt32(2),
                                pos_width = reader.GetInt32(3),
                                pos_height = reader.GetInt32(4)
                            });
                        }
                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }

            }
            return BoxID;
        }

        //Shaun : save all crop and converted text result to DB
        public void InsertCropResult(List<CropResult> CropImageRes)
        {
            DateTime date = DateTime.Now;
            string sql = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();

                    for (int i = 0; i < CropImageRes.Count; i++)
                    {
                        sql = "INSERT INTO Form_ImageCrop VALUES (@FormKey,@Imgpath,@ImgText,@Cre_Date,@Cre_By)";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@FormKey", CropImageRes[i].FormID_Key);
                            cmd.Parameters.AddWithValue("@Imgpath", CropImageRes[i].Crop_Imgpath);
                            cmd.Parameters.AddWithValue("@ImgText", CropImageRes[i].Crop_Text);
                            cmd.Parameters.AddWithValue("@Cre_Date", date);
                            cmd.Parameters.AddWithValue("@Cre_By", "SCRIPT");
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
        }

        //Shaun : retrieve all details to display on listing form
        public List<Listing> getListing()
        {
            var ListID = new List<Listing>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT [ID],[Form_Reference],[Created_Date] FROM [Form_Image] ORDER BY [ID] DESC";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            ListID.Add(new Listing
                            {
                                ID = reader.GetInt32(0),
                                Reference = reader.GetString(1),
                                CreationDate = reader.GetDateTime(2)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                return ListID;
            }
        }

        //Shaun : retreive the particular information to display on detail form
        public List<getDetails> getDetailList(string FormImgid)
        {
            var DetailList = new List<getDetails>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    //sqlBox = "SELECT B.[ID],A.[ID],A.[Form_Reference],A.[page_no],A.[Form_Path],B.[Crop_Imgpath],B.[Crop_Text] FROM [Form_Image] A INNER JOIN [Form_ImageCrop] B ON A.ID = B.FormID_key where A.Form_Reference = '" + FormImgid + "'";
                    sqlBox = "SELECT B.[ID],A.[ID],A.[Form_Reference],A.[page_no],A.[Form_Path],B.[Crop_Imgpath],B.[Crop_Text],A.[Form_ID] FROM [Form_Image] A INNER JOIN [Form_ImageCrop] B ON A.ID = B.FormID_key where A.Form_Reference = '" + FormImgid + "'";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            DetailList.Add(new getDetails
                            {
                                CropID = reader.GetInt32(0),
                                FormID = reader.GetInt32(1),
                                Reference = reader.GetString(2),
                                pageno = reader.GetInt32(3),
                                Form_Path = reader.GetString(4),
                                Crop_Imgpath = reader.GetString(5),
                                Crop_Text = reader.GetString(6).Replace("\\r\\n", Environment.NewLine),
                                formid = reader.GetInt32(7)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                return DetailList;
            }
        }

        //Chai: added to get upload images
        public List<getDetails> getOriFile(string FormImgid)
        {
            var DetailList = new List<getDetails>();
            string sqlBox = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sqlBox = "SELECT [ID],[Form_Reference],[Form_Path],[page_no] FROM [Form_Image] where Form_Reference = '" + FormImgid + "'";

                    using (SqlCommand cmd = new SqlCommand(sqlBox, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            DetailList.Add(new getDetails
                            {
                                FormID = reader.GetInt32(0),
                                Reference = reader.GetString(1),
                                Form_Path = reader.GetString(2),
                                pageno = reader.GetInt32(3)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
                return DetailList;
            }
        }

        //Shaun: save updated text from detail form
        public void UpdateDetailInfoModel(List<string> TextCrop, List<string> CropID)
        {
            string sql = null;

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();

                    for (int i = 0; i < CropID.Count; i++)
                    {
                        sql = "UPDATE Form_ImageCrop SET Crop_TEXT = @Textcrop WHERE ID = @CropID";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Textcrop", TextCrop[i].ToString());
                            cmd.Parameters.AddWithValue("@CropID", CropID[i]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }
        }

        //Shaun [16072018]: Get DropDown Value from DB
        public List<dropdownForm> GetDDLForm()
        {
            var ListDDL = new List<dropdownForm>();
            string sql = "";

            string constring = ConfigurationManager.ConnectionStrings["OCRDB"].ToString();
            con = new SqlConnection(constring);

            using (SqlConnection conn = new SqlConnection(constring))
            {
                try
                {
                    conn.Open();
                    sql = "SELECT Value,Value2 FROM FORM_COMMON WHERE REFERENCE ='DDLInfo'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            ListDDL.Add(new dropdownForm
                            {
                                Text = reader.GetString(0),
                                Value = reader.GetString(1),
                            });
                        }
                        reader.Close();
                    }

                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                finally
                {
                    con.Close();
                }
            }

            return ListDDL;
        }

    }
}