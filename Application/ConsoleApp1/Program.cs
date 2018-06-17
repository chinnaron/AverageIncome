using System;
using GdPicture14;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = Directory.GetCurrentDirectory();
            var fileNames = Directory.GetFiles(dir + "\\In");
            foreach (var fileName in fileNames)
            {
                //Check file format
                var fileFormat = fileName.Split('\\').LastOrDefault().Split('.');
                var name = fileFormat.FirstOrDefault();
                var format = fileFormat.LastOrDefault();
                if (string.IsNullOrEmpty(format)
                    || !format.Equals("pdf", StringComparison.CurrentCultureIgnoreCase)
                    || fileFormat.Count() != 2)
                    continue;

                var oGdPicturePDF = new GdPicturePDF();
                var status = oGdPicturePDF.LoadFromFile(fileName, false);
                if (status == GdPictureStatus.OK)
                {
                    var encoding = Encoding.GetEncoding("windows-874");
                    using (FileStream fs = new FileStream(dir + "\\Out\\" + name + ".csv", FileMode.Create))
                    {
                        using (StreamWriter text_file = new StreamWriter(fs, encoding))
                        {
                            try
                            {
                                var pageCount = oGdPicturePDF.GetPageCount();
                                status = oGdPicturePDF.GetStat();
                                if (status == GdPictureStatus.OK)
                                {
                                    text_file.WriteLine("Account No.,Account Name,Form,To,Transaction Date,Transaction Description,Deposit/Withdrawal,Transaction Amount,Remaining Balance,Remark");
                                    //string page_text = null;
                                    for (int i = 1; i <= pageCount; i++)
                                    {
                                        status = oGdPicturePDF.SelectPage(i);
                                        if (status == GdPictureStatus.OK)
                                        {
                                            var transactionLines = new List<Model>();
                                            var page_text = oGdPicturePDF.GetPageText();
                                            status = oGdPicturePDF.GetStat();
                                            if (status == GdPictureStatus.OK)
                                            {
                                                //Change page_text to transaction lines
                                                var stringSeparators = new char[] { '\r', '\n' };
                                                var lines = page_text.Split(stringSeparators);

                                                //Separate pdf 1 and 2
                                                if (name.Contains("1"))
                                                {
                                                    var model = new Model();
                                                    var loop = false;
                                                    var lastLine = "";
                                                    foreach (var line in lines)
                                                    {
                                                        if (string.IsNullOrEmpty(line.Trim())) continue;
                                                        //text_file.WriteLine(line);
                                                        //AccountNo & AccountName
                                                        if (line.Contains("Account"))
                                                        {
                                                            var lineTemps = line.Split(' ');
                                                            for (var j = 0; j < lineTemps.Length; j++)
                                                            {
                                                                if (CheckFirstIdNum(lineTemps[j].Trim(), 0, 1))
                                                                {
                                                                    model.AccountNo = Int32.Parse(lineTemps[j]);
                                                                    break;
                                                                }
                                                            }
                                                            var idx = line.Trim().IndexOf("Name");
                                                            model.AccountName = line.Substring(idx + 5).Trim();
                                                        }

                                                        //From & To
                                                        else if (line.Contains("/") && line.Contains("Date"))
                                                        {
                                                            var lineTemps = line.Split(' ');
                                                            var dateLines = lineTemps.Where(m => m.Contains("/") && m.Length > 4);
                                                            var dateFrom = dateLines.First().Trim().Split('/').ToList();
                                                            var dateTo = dateLines.Last().Trim().Split('/').ToList();
                                                            model.From = new DateTime(Int32.Parse(dateFrom[2]), Int32.Parse(dateFrom[1]), Int32.Parse(dateFrom[0]));
                                                            model.To = new DateTime(Int32.Parse(dateTo[2]), Int32.Parse(dateTo[1]), Int32.Parse(dateTo[0]));
                                                        }

                                                        //Else
                                                        else
                                                        {
                                                            var firstIsNum = CheckFirstIdNum(line.Trim(), 0, 4);

                                                            if (line.Contains("/") && firstIsNum)
                                                            {
                                                                loop = !loop;
                                                            }
                                                            if (loop)
                                                            {
                                                                lastLine += line;
                                                                continue;
                                                            }
                                                            if (string.IsNullOrEmpty(lastLine)) continue;

                                                            var lineTemp = lastLine.Trim().Split(' ');
                                                            var date = lineTemp[0].Trim().Split('/').ToList();
                                                            model.TxnDate = new DateTime(Int32.Parse(date[2]), Int32.Parse(date[1]), Int32.Parse(date[0]));
                                                            var idx = 2;
                                                            model.Remark = "Tr Code " + lineTemp[idx];
                                                            idx++;
                                                            if (model.Remark.Contains("IN") || model.Remark.Contains("C1") 
                                                                || model.Remark.Contains("CD") || model.Remark.Contains("X1"))
                                                                model.Type = "Deposit";
                                                            else
                                                                model.Type = "Withdrawal";
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                if (lineTemp[j].Contains("."))
                                                                {
                                                                    model.TxnAmount = float.Parse(lineTemp[j].Replace(",", ""));
                                                                    idx = j + 1;
                                                                    break;
                                                                }
                                                            }
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                if (lineTemp[j].Contains("."))
                                                                {
                                                                    model.RemainingBalance = float.Parse(lineTemp[j].Replace(",", ""));
                                                                    idx = j + 1;
                                                                    break;
                                                                }
                                                            }
                                                            model.TxnDescription = "";
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                model.TxnDescription += lineTemp[j];
                                                            }

                                                            transactionLines.Add(model);
                                                            text_file.WriteLine(model.ToLineString());
                                                            lastLine = line;
                                                            loop = true;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var model = new Model();
                                                    var loop = false;
                                                    var lastLine = "";
                                                    foreach (var line in lines)
                                                    {
                                                        if (string.IsNullOrEmpty(line.Trim())) continue;
                                                        //text_file.WriteLine(line);
                                                        //AccountNo
                                                        if (line.Contains("�Ţ���ѭ��"))
                                                        {
                                                            var lineTemps = line.Split(' ');
                                                            model.AccountNo = Int32.Parse(lineTemps.LastOrDefault().Trim().Replace("-", ""));
                                                        }

                                                        //AccountName
                                                        else if (line.Contains("���ͺѭ��"))
                                                        {
                                                            var idx = line.Trim().IndexOf(' ');
                                                            model.AccountName = line.Substring(idx).Trim();
                                                        }

                                                        //From & To
                                                        else if (line.Contains("/") && line.Contains("-") && line.Contains("����"))
                                                        {
                                                            var lineTemps = line.Split(' ');
                                                            var dateLines = lineTemps.Where(m => m.Contains("/"));
                                                            var dateFrom = dateLines.First().Trim().Split('/').ToList();
                                                            var dateTo = dateLines.Last().Trim().Split('/').ToList();
                                                            model.From = new DateTime(Int32.Parse(dateFrom[2]), Int32.Parse(dateFrom[1]), Int32.Parse(dateFrom[0]));
                                                            model.To = new DateTime(Int32.Parse(dateTo[2]), Int32.Parse(dateTo[1]), Int32.Parse(dateTo[0]));
                                                        }

                                                        //Else
                                                        else
                                                        {
                                                            var firstIsNum = CheckFirstIdNum(line.Trim(), 0, 4);

                                                            if (line.Contains("/") && firstIsNum)
                                                            {
                                                                loop = !loop;
                                                            }
                                                            if (loop)
                                                            {
                                                                lastLine += line;
                                                                continue;
                                                            }
                                                            if (string.IsNullOrEmpty(lastLine)) continue;

                                                            var lineTemp = lastLine.Trim().Split(' ');
                                                            var date = lineTemp[0].Trim().Split('/').ToList();
                                                            model.TxnDate = new DateTime(Int32.Parse(date[2]), Int32.Parse(date[1]), Int32.Parse(date[0]));
                                                            var txnDes = "";
                                                            var idx = 2;
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                if (CheckFirstIdNum(lineTemp[j].Trim(), 0, 1))
                                                                {
                                                                    idx = j;
                                                                    break;
                                                                }
                                                                txnDes += lineTemp[j];
                                                            }
                                                            model.TxnDescription = txnDes;
                                                            model.Type = txnDes.Contains("�ҡ") ? "Deposit" : "Withdrawal";
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                if (lineTemp[j].Contains("."))
                                                                {
                                                                    model.TxnAmount = float.Parse(lineTemp[j].Replace(",", ""));
                                                                    idx = j + 1;
                                                                    break;
                                                                }
                                                            }
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                if (lineTemp[j].Contains("."))
                                                                {
                                                                    model.RemainingBalance = float.Parse(lineTemp[j].Replace(",", ""));
                                                                    idx = j + 1;
                                                                    break;
                                                                }
                                                            }
                                                            model.Remark = "";
                                                            for (var j = idx; j < lineTemp.Length; j++)
                                                            {
                                                                model.Remark += lineTemp[j];
                                                            }

                                                            transactionLines.Add(model);
                                                            text_file.WriteLine(model.ToLineString());
                                                            lastLine = line;
                                                            loop = true;
                                                        }
                                                    }
                                                }

                                                //Average income
                                                text_file.WriteLine("\nAvarage Income," + AvgIncome(transactionLines));
                                            }
                                        }
                                    }
                                    //text_file.Write("\n");
                                }
                                text_file.Close();
                            }
                            catch (Exception e)
                            {
                                text_file.Close();
                                throw new Exception(e.Message);
                            }
                        }
                    }


                }
                oGdPicturePDF.Dispose();
            }
        }

        private static bool CheckFirstIdNum(string s, int idx, int length)
        {
            try
            {
                Int32.Parse(s.Trim().Substring(idx, length).Replace("/", ""));
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private static float AvgIncome(List<Model> models)
        {
            try
            {
                var idx = 0;
                var count = 0;
                float sum = 0;
                var previous = new List<Model>() { models[idx] };
                for (var j = idx + 1; j < models.Count; j++)
                {
                    if (models[j].TxnDate > previous.FirstOrDefault().TxnDate)
                    {
                        idx = j;
                        break;
                    }
                    previous.Add(models[j]);
                }
                var current = new List<Model>() { models[idx] };
                for (var j = idx + 1; j < models.Count; j++)
                {
                    if (models[j].TxnDate > current.FirstOrDefault().TxnDate)
                    {
                        idx = j;
                        break;
                    }
                    current.Add(models[j]);
                }
                for (var j = idx; j < models.Count; j++)
                {
                    var balance = current[0].RemainingBalance;
                    for (var k = 0; k < previous.Count; k++)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                return 0;
            }
            return 0;
        }
    }
}
