using log4net;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DataMigration_Report
{
    public partial class Form1 : Form
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(Form1));

        //参照パス
        private string selectedPath = "";

        //exe.config
        private string dbUserId = "";
        private string dbPassword = "";
        private string dbSource = "";
        private string dateMode = "";
        private string keyImagePath = "";

        //データ型不一致カウンター
        private int validationErrorCount = 0;

        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                //exe.config設定
                dbUserId = ConfigurationManager.AppSettings["dbUserId"];
                dbPassword = ConfigurationManager.AppSettings["dbPassword"];
                dbSource = ConfigurationManager.AppSettings["dbSource"];
                dateMode = ConfigurationManager.AppSettings["dateMode"];
                keyImagePath = ConfigurationManager.AppSettings["keyImagePath"];

                //対象日を画面に反映
                switch (dateMode)
                {
                    case "取込日":
                        execLabel.Text = "対象取込日：";
                        break;
                    case "検査日":
                    default:
                        execLabel.Text = "対象検査日：";
                        break;
                }

                AppendLog("");
                AppendLog("アプリケーションが初期化されました");
            }
            catch (Exception ex)
            {
                AppendLog("設定ファイルの読み込みに失敗しました: " + ex.Message, "ERROR");
                MessageBox.Show("設定ファイルの読み込みに失敗しました: " + ex.Message);
            }
        }

        //参照選択
        private void dataOpenFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    selectedPath = fbd.SelectedPath;
                    dataPath.Text = selectedPath;
                    AppendLog("参照ボタン", "INFO");
                    //ファイルを探す
                    scanFolderAndCount("scanOnly");
                }
            }
        }

        //取込ボタン
        private void dataButton_Click(object sender, EventArgs e)
        {
            AppendLog("取込ボタン", "INFO");
            //ファイルを探す + 一次に移行
            scanFolderAndCount("insertIntoDB");
        }


        //ファイルを探す
        private async void scanFolderAndCount(String mode)
        {
            validationErrorCount = 0;

            //参照パス取得
            string path = dataPath.Text;

            //DB conn
            string connString = $"User Id={dbUserId};Password={dbPassword};Data Source={dbSource};";


            if (!Directory.Exists(path))
            {
                AppendLog("選択したフォルダが見つかりません", "ERROR");
                MessageBox.Show("選択したフォルダが見つかりません");
                return;
            }

            AppendLog($"{path}のスキャンを開始します");

            try
            {
                //XMLファイルを探す
                var files = await Task.Run(() =>
                    Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories)
                );

                AppendLog($"XMLファイルを{files.Length}件検出しました。", "INFO");

                //ログ用カウンター
                var commitOKCount = 0;
                var commitNGCount = 0;

                //ダイアローグ用フラグ
                var checkFailed = false;

                await Task.Run(() =>
                {

                    using (OracleConnection conn = new OracleConnection(connString))
                    {
                        if (mode == "insertIntoDB") conn.Open();
                        foreach (var file in files)
                        {
                            try
                            {
                                bool isValid = true;

                                string fileName = Path.GetFileName(file);
                                // AppendLog($"検証中：{fileName}");

                                var xDoc = XDocument.Load(file);

                                // XMLノードをチェック
                                string[] requiredNodes = {
                                "Report",
                                "OrderNumber",
                                "AccessionNumber",
                                "StudyinstanceUID",
                                "OrderDateTime",
                                "ReferringDepartmentName",
                                "ReferringPhysician",
                                "ClinicalInformation",
                                "StudyInformation",
                                "StudyPointTime",
                                "Modality",
                                "BodyPart",
                                "HospitalWard",
                                "OperatorName",
                                "OperatorComment",
                                "Patient",
                                "PatientID",
                                "PatientName",
                                "PatientKana",
                                "PatientKanji",
                                "PatientSex",
                                "PatientBirthDate",
                                "PatientStatus",
                                "Study",
                                "StudyDateTime",
                                "StudyDescription",
                                "DiagnosingUser",
                                "DiagnosedDateTime",
                                "RevisingUser",
                                "RevCloseDateTime",
                                "ApprovingUser",
                                "ApprovedDateTime",
                                "Finding",
                                "Diagnosis",
                                "Conclusion",
                                "KeyImage",
                                "Key_Number",
                                "Key_Title",
                                "Key_Type",
                                "Key_File",
                            };

                                foreach (var nodeName in requiredNodes)
                                {
                                    if (xDoc.Descendants(nodeName).Any() == false)
                                    {
                                        AppendLog($"ファイル{fileName}に必要なノード<{nodeName}>が見つかりません", "ERROR");
                                        isValid = false;
                                    }
                                }

                                //キーを確認
                                string orderNumber = xDoc.Root.Element("OrderNumber")?.Value ?? "";
                                string accessionNumber = xDoc.Root.Element("AccessionNumber")?.Value ?? "";
                                string patientID = xDoc.XPathSelectElement("//Patient/PatientID")?.Value ?? "";
                                string studyDateTime = xDoc.XPathSelectElement("//Study/StudyDateTime")?.Value ?? "";

                                if (string.IsNullOrEmpty(orderNumber) || string.IsNullOrEmpty(accessionNumber) || string.IsNullOrEmpty(patientID) || string.IsNullOrEmpty(studyDateTime))
                                {
                                    AppendLog($"ファイル{fileName}にOrderNumber,AccessionNumber,PatientID,StudyDateTimeいずれが含まれていません", "ERROR");
                                    isValid = false;
                                }

                                if (!isValid)
                                {
                                    checkFailed = true;
                                    commitNGCount++;
                                }
                                else
                                {
                                    if (mode == "insertIntoDB")
                                    {
                                        // Start transaction
                                        using (OracleTransaction trans = conn.BeginTransaction())
                                        {
                                            try
                                            {
                                                executeMerge(conn, trans, xDoc);
                                                trans.Commit();
                                                commitOKCount++;
                                                AppendLog($"{fileName}（注文番号：{orderNumber}取込完了しました。{commitOKCount}/{files.Length}件）", "INFO");
                                            }
                                            catch (Exception dbEx)
                                            {
                                                AppendLog($"{fileName}の取込に失敗しました。{dbEx.Message}", "ERROR");
                                                commitNGCount++;
                                                checkFailed = true;
                                            }
                                        }// End of transaction block
                                    }// End of mode check
                                }// End of isValid check
                            }
                            catch (Exception ex)
                            {
                                AppendLog($"XML解析エラー発生しました：{ex.Message}");
                                checkFailed = true;
                            }
                        }// End of file loop
                    }// End of using connection
                });
                if (checkFailed == true)
                {
                    MessageBox.Show($"エラーが発生しました。ログをご確認ください");
                    checkFailed = false;
                }
                else
                {
                    if (mode == "insertIntoDB")
                    {
                        string logMessage = $"取込完了しました、取込件数：OK： {commitOKCount}件/NG： {commitNGCount}件";
                        string boxMessage = $"取込完了しました、取込件数： {commitOKCount}件";
                        if (validationErrorCount > 0)
                        {
                            logMessage += $" (うち、データ型不一致が{validationErrorCount}件)";
                            boxMessage += $"\nデータ型不一致が{validationErrorCount}件ありました。詳細はログを確認してください。";
                        }
                        AppendLog(logMessage, "INFO");
                        MessageBox.Show(boxMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{ex.Message}", "ERROR");
            }
        }

        //一次テーブルのデータを確認 - 日付形
        private object GetValidDateString(string dateString, string[] formats, string label)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return DBNull.Value;
            }
            DateTime temp;
            if (DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out temp))
            {
                return dateString;
            }
            AppendLog($"{label}:{dateString}, 正しい日付形が必要です。期待される形式：'{string.Join(" or ", formats)}'。NULLとして保存します。", "WARN");
            validationErrorCount++;
            return DBNull.Value;
        }

        //一次テーブルのデータを確認 - Int形
        private object GetValidIntString(string intString, string label)
        {
            if (string.IsNullOrEmpty(intString))
            {
                return DBNull.Value;
            }
            int temp;
            if (int.TryParse(intString, out temp))
            {
                return intString;
            }
            AppendLog($"{label}：{intString}, 整数型が必要です。NULLとして保存します。", "WARN");
            validationErrorCount++;
            return DBNull.Value;
        }

        //XMLファイルから一次テーブルに保存
        private void executeMerge(OracleConnection conn, OracleTransaction trans, XDocument xDoc)
        {

            var keyImages = xDoc.XPathSelectElements("//KeyImage").ToList();
            StringBuilder updateKeys = new StringBuilder();
            StringBuilder insertKeysCols = new StringBuilder();
            StringBuilder insertKeysVals = new StringBuilder();

            for (int i = 0; i < keyImages.Count; i++)
            {
                int idx = i + 1;
                updateKeys.Append($", target.Key_Number_{idx} = :Key_Number_{idx}");
                updateKeys.Append($", target.Key_Title_{idx} = :Key_Title_{idx}");
                updateKeys.Append($", target.Key_Type_{idx} = :Key_Type_{idx}");
                updateKeys.Append($", target.Key_File_{idx} = :Key_File_{idx}");

                insertKeysCols.Append($", Key_Number_{idx}, Key_Title_{idx}, Key_Type_{idx}, Key_File_{idx}");
                insertKeysVals.Append($", :Key_Number_{idx}, :Key_Title_{idx}, :Key_Type_{idx}, :Key_File_{idx}");
            }

            string sql = $@"
            MERGE INTO IKOU.IKOU_TEMP_TABLE target
            USING (SELECT :OrderNumber AS src_orderNo FROM DUAL) source
            ON (target.orderNumber = source.src_orderNo)
            WHEN MATCHED THEN
                UPDATE SET
                target.AccessionNumber = :AccessionNumber,
                target.StudyinstanceUID = :StudyinstanceUID,
                target.OrderDateTime = :OrderDateTime,
                target.ReferringDepartmentName = :ReferringDepartmentName,
                target.ReferringPhysician = :ReferringPhysician,
                target.ClinicalInformation = :ClinicalInformation,
                target.StudyInformation = :StudyInformation,
                target.StudyPointTime = :StudyPointTime,
                target.Modality = :Modality,
                target.BodyPart = :BodyPart,
                target.HospitalWard = :HospitalWard,
                target.OperatorName = :OperatorName,
                target.OperatorComment = :OperatorComment,
                target.PatientID = :PatientID,
                target.PatientName = :PatientName,
                target.PatientKana = :PatientKana,
                target.PatientKanji = :PatientKanji,
                target.PatientSex = :PatientSex,
                target.PatientBirthDate = :PatientBirthDate,
                target.PatientStatus = :PatientStatus,
                target.StudyDateTime = :StudyDateTime,
                target.StudyDescription = :StudyDescription,
                target.DiagnosingUser = :DiagnosingUser,
                target.DiagnosedDateTime = :DiagnosedDateTime,
                target.RevisingUser = :RevisingUser,
                target.RevCloseDateTime = :RevCloseDateTime,
                target.ApprovingUser = :ApprovingUser,
                target.ApprovedDateTime = :ApprovedDateTime,
                target.Finding = :Finding,
                target.Diagnosis = :Diagnosis,
                target.Conclusion = :Conclusion,
                target.torikomi_date = SYSDATE,
                target.ikou_date = :ikou_date,
                target.ikou_result = :ikou_result,
                target.ikou_text = :ikou_text{updateKeys}
            WHEN NOT MATCHED THEN

                INSERT (OrderNumber, AccessionNumber, StudyinstanceUID, OrderDateTime, ReferringDepartmentName, ReferringPhysician, ClinicalInformation, StudyInformation, StudyPointTime, Modality, BodyPart, HospitalWard, OperatorName, OperatorComment, PatientID, PatientName, PatientKana, PatientKanji, PatientSex, PatientBirthDate, PatientStatus, StudyDateTime, StudyDescription, DiagnosingUser, DiagnosedDateTime, RevisingUser, RevCloseDateTime, ApprovingUser, ApprovedDateTime, Finding, Diagnosis, Conclusion, torikomi_date, ikou_date, ikou_result, ikou_text{insertKeysCols})

                VALUES (:OrderNumber, :AccessionNumber, :StudyinstanceUID, :OrderDateTime, :ReferringDepartmentName, :ReferringPhysician, :ClinicalInformation, :StudyInformation, :StudyPointTime, :Modality, :BodyPart, :HospitalWard, :OperatorName, :OperatorComment, :PatientID, :PatientName, :PatientKana, :PatientKanji, :PatientSex, :PatientBirthDate, :PatientStatus, :StudyDateTime, :StudyDescription, :DiagnosingUser, :DiagnosedDateTime, :RevisingUser, :RevCloseDateTime, :ApprovingUser, :ApprovedDateTime, :Finding, :Diagnosis, :Conclusion, SYSDATE, :ikou_date, :ikou_result, :ikou_text{insertKeysVals})";

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = trans;
                cmd.BindByName = true;

                //日付形式確認用フォーマット, 必要時にH:mm:ssも追加可能
                string[] dateTimeFormats = { "yyyy/MM/dd HH:mm:ss" };//,"yyyy/MM/dd H:mm:ss"
                string[] dateFormat = { "yyyy/MM/dd" };

                cmd.Parameters.Add("OrderNumber", OracleDbType.Varchar2).Value = xDoc.Root.Element("OrderNumber")?.Value ?? "";
                cmd.Parameters.Add("AccessionNumber", OracleDbType.Varchar2).Value = xDoc.Root.Element("AccessionNumber")?.Value ?? "";
                cmd.Parameters.Add("StudyinstanceUID", OracleDbType.Varchar2).Value = xDoc.Root.Element("StudyinstanceUID")?.Value ?? "";
                cmd.Parameters.Add("OrderDateTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.Root.Element("OrderDateTime")?.Value, dateTimeFormats, "OrderDateTime");
                cmd.Parameters.Add("ReferringDepartmentName", OracleDbType.Varchar2).Value = xDoc.Root.Element("ReferringDepartmentName")?.Value ?? "";
                cmd.Parameters.Add("ReferringPhysician", OracleDbType.Varchar2).Value = xDoc.Root.Element("ReferringPhysician")?.Value ?? "";
                cmd.Parameters.Add("ClinicalInformation", OracleDbType.Varchar2).Value = xDoc.Root.Element("ClinicalInformation")?.Value ?? "";
                cmd.Parameters.Add("StudyInformation", OracleDbType.Varchar2).Value = xDoc.Root.Element("StudyInformation")?.Value ?? "";
                cmd.Parameters.Add("StudyPointTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.Root.Element("StudyPointTime")?.Value, dateTimeFormats, "StudyPointTime");
                cmd.Parameters.Add("Modality", OracleDbType.Varchar2).Value = xDoc.Root.Element("Modality")?.Value ?? "";
                cmd.Parameters.Add("BodyPart", OracleDbType.Varchar2).Value = xDoc.Root.Element("BodyPart")?.Value ?? "";
                cmd.Parameters.Add("HospitalWard", OracleDbType.Varchar2).Value = xDoc.Root.Element("HospitalWard")?.Value ?? "";
                cmd.Parameters.Add("OperatorName", OracleDbType.Varchar2).Value = xDoc.Root.Element("OperatorName")?.Value ?? "";
                cmd.Parameters.Add("OperatorComment", OracleDbType.Varchar2).Value = xDoc.Root.Element("OperatorComment")?.Value ?? "";

                cmd.Parameters.Add("PatientID", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Patient/PatientID")?.Value ?? "";
                cmd.Parameters.Add("PatientName", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Patient/PatientName")?.Value ?? "";
                cmd.Parameters.Add("PatientKana", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Patient/PatientKana")?.Value ?? "";
                cmd.Parameters.Add("PatientKanji", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Patient/PatientKanji")?.Value ?? "";
                cmd.Parameters.Add("PatientSex", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Patient/PatientSex")?.Value ?? "";
                cmd.Parameters.Add("PatientBirthDate", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.XPathSelectElement("//Patient/PatientBirthDate")?.Value, dateFormat, "PatientBirthDate");
                cmd.Parameters.Add("PatientStatus", OracleDbType.Varchar2).Value = GetValidIntString(xDoc.XPathSelectElement("//Patient/PatientStatus")?.Value, "PatientStatus");
                cmd.Parameters.Add("StudyDateTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.XPathSelectElement("//Study/StudyDateTime")?.Value, dateTimeFormats, "StudyDateTime");
                cmd.Parameters.Add("StudyDescription", OracleDbType.Varchar2).Value = xDoc.XPathSelectElement("//Study/StudyDescription")?.Value ?? "";
                cmd.Parameters.Add("DiagnosingUser", OracleDbType.Varchar2).Value = xDoc.Root.Element("DiagnosingUser")?.Value ?? "";
                cmd.Parameters.Add("DiagnosedDateTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.Root.Element("DiagnosedDateTime")?.Value, dateTimeFormats, "DiagnosedDateTime");
                cmd.Parameters.Add("RevisingUser", OracleDbType.Varchar2).Value = xDoc.Root.Element("RevisingUser")?.Value ?? "";
                cmd.Parameters.Add("RevCloseDateTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.Root.Element("RevCloseDateTime")?.Value, dateTimeFormats, "RevCloseDateTime");
                cmd.Parameters.Add("ApprovingUser", OracleDbType.Varchar2).Value = xDoc.Root.Element("ApprovingUser")?.Value ?? "";
                cmd.Parameters.Add("ApprovedDateTime", OracleDbType.Varchar2).Value = GetValidDateString(xDoc.Root.Element("ApprovedDateTime")?.Value, dateTimeFormats, "ApprovedDateTime");
                cmd.Parameters.Add("Finding", OracleDbType.Varchar2).Value = xDoc.Root.Element("Finding")?.Value ?? "";
                cmd.Parameters.Add("Diagnosis", OracleDbType.Varchar2).Value = xDoc.Root.Element("Diagnosis")?.Value ?? "";
                cmd.Parameters.Add("Conclusion", OracleDbType.Varchar2).Value = xDoc.Root.Element("Conclusion")?.Value ?? "";

                cmd.Parameters.Add("ikou_date", OracleDbType.Date).Value = DBNull.Value;
                cmd.Parameters.Add("ikou_result", OracleDbType.Varchar2).Value = "";
                cmd.Parameters.Add("ikou_text", OracleDbType.Long).Value = "";

                for (int i = 0; i < keyImages.Count; i++)
                {
                    var img = keyImages[i];
                    int keyIndex = i + 1;
                    cmd.Parameters.Add("Key_Number_" + keyIndex, OracleDbType.Varchar2).Value = GetValidIntString(img.Element("Key_Number")?.Value, "Key_Number");
                    cmd.Parameters.Add("Key_Title_" + keyIndex, OracleDbType.Varchar2).Value = img.Element("Key_Title")?.Value ?? "";
                    cmd.Parameters.Add("Key_Type_" + keyIndex, OracleDbType.Varchar2).Value = img.Element("Key_Type")?.Value ?? "";
                    cmd.Parameters.Add("Key_File_" + keyIndex, OracleDbType.Varchar2).Value = img.Element("Key_File")?.Value ?? "";
                }
                cmd.ExecuteNonQuery();
            }
        }


        //対象日からの選択
        private void dateTimePickerFrom_ValueChanged(object sender, EventArgs e)
        {
            showIkouTableCount();
        }
        //対象日までの選択
        private void dateTimePickerTo_ValueChanged(object sender, EventArgs e)
        {
            showIkouTableCount();
        }

        //一次テーブルに対象日件数を調べる (開発中にテストとログ用)
        private void showIkouTableCount()
        {
            try
            {
                DateTime fromDate = dateTimePickerFrom.Value.Date;
                DateTime toDate = dateTimePickerTo.Value.Date.AddDays(1); // Next day midnight for inclusive range

                string whereClause;
                if (dateMode == "取込日")
                {
                    whereClause = "torikomi_date >= :fromDate AND torikomi_date < :toDate";
                }
                else
                {
                    whereClause = "TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') >= :fromDate AND TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') < :toDate";
                }

                string connString = $"User Id={dbUserId};Password={dbPassword};Data Source={dbSource};";

                using (OracleConnection conn = new OracleConnection(connString))
                {
                    conn.Open();
                    string sql = $"SELECT COUNT(*) FROM IKOU.IKOU_TEMP_TABLE WHERE {whereClause}";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate;
                        cmd.Parameters.Add("toDate", OracleDbType.Date).Value = toDate;

                        object result = cmd.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;

                        AppendLog($"対象日選択：{dateMode}：{fromDate}～{toDate}。移行件数：{count}件");

                        if (resultCount.InvokeRequired)
                        {
                            resultCount.Invoke(new Action(() => resultCount.Text = $"移行件数：{count}件"));
                        }
                        else
                        {
                            resultCount.Text = $"移行件数：{count}件";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"件数確認エラー: {ex.Message}", "ERROR");
                MessageBox.Show($"件数確認中にエラーが発生しました: {ex.Message}");
            }
        }

        //移行完了後、IDを使って結果を確認 -> 一次テーブルに結果を更新
        private void checkAndAppendIkouTableResult()
        {
            try
            {
                DateTime fromDate = dateTimePickerFrom.Value.Date;
                DateTime toDate = dateTimePickerTo.Value.Date.AddDays(1); // Next day midnight for inclusive range

                string whereClause;
                if (dateMode == "取込日")
                {
                    whereClause = "torikomi_date >= :fromDate AND torikomi_date < :toDate";
                }
                else
                {
                    whereClause = "TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') >= :fromDate AND TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') < :toDate";
                }

                string connString = $"User Id={dbUserId};Password={dbPassword};Data Source={dbSource};";

                //ikouTable読み込み用
                using (OracleConnection ikouTableConn = new OracleConnection(connString))
                //command読み込み用
                using (OracleConnection commandConn = new OracleConnection(connString))
                {
                    ikouTableConn.Open();
                    commandConn.Open();

                    // Build SELECT for all keys
                    StringBuilder sbSql = new StringBuilder();
                    sbSql.Append("SELECT PatientID, OrderNumber");
                    for (int i = 1; i <= 20; i++)
                    {
                        sbSql.Append($", Key_Number_{i}");
                    }
                    sbSql.Append($" FROM IKOU.IKOU_TEMP_TABLE WHERE {whereClause}");

                    using (OracleCommand cmd = new OracleCommand(sbSql.ToString(), ikouTableConn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate;
                        cmd.Parameters.Add("toDate", OracleDbType.Date).Value = toDate;

                        using (OracleDataReader ikouTableReader = cmd.ExecuteReader())
                        {

                            //移行チェック
                            using (OracleCommand command_patientinfo = new OracleCommand("SELECT 1 FROM MRMS.PATIENTINFO WHERE ID = :id", commandConn))
                            using (OracleCommand command_examinfo = new OracleCommand("SELECT 1 FROM MRMS.EXAMINFO WHERE ID = :id", commandConn))
                            using (OracleCommand command_exambuinfo = new OracleCommand("SELECT 1 FROM MRMS.EXAMBUIINFO WHERE ID = :id", commandConn))
                            using (OracleCommand command_alertcomment = new OracleCommand("SELECT 1 FROM MRMS.ALERTCOMMENT WHERE ID = :id", commandConn))
                            using (OracleCommand command_reportinfo = new OracleCommand("SELECT 1 FROM MRMS.REPORTINFO WHERE ID = :id", commandConn))
                            using (OracleCommand command_imageinfo = new OracleCommand("SELECT 1 FROM MRMS.IMAGEINFO WHERE ID = :id AND SHOWORDER = :showOrder", commandConn))


                            //ikou_tableのikou_result,ikou_textを更新
                            using (OracleCommand command_ikou_update = new OracleCommand("UPDATE IKOU.IKOU_TEMP_TABLE SET ikou_date = SYSDATE, ikou_result = :ikou_result,ikou_text = :ikou_text WHERE OrderNumber = :orderNumber", commandConn))
                            {
                                command_patientinfo.Parameters.Add("id", OracleDbType.Varchar2);
                                command_examinfo.Parameters.Add("id", OracleDbType.Varchar2);
                                command_exambuinfo.Parameters.Add("id", OracleDbType.Varchar2);
                                command_alertcomment.Parameters.Add("id", OracleDbType.Varchar2);
                                command_reportinfo.Parameters.Add("id", OracleDbType.Varchar2);

                                command_imageinfo.Parameters.Add("id", OracleDbType.Varchar2);
                                command_imageinfo.Parameters.Add("showOrder", OracleDbType.Varchar2);

                                command_ikou_update.Parameters.Add("ikou_result", OracleDbType.Varchar2);
                                command_ikou_update.Parameters.Add("ikou_text", OracleDbType.Varchar2);
                                command_ikou_update.Parameters.Add("orderNumber", OracleDbType.Varchar2);

                                int passedCount = 0;
                                int errorCount = 0;

                                AppendLog("移行結果チェックを開始します...", "INFO");

                                while (ikouTableReader.Read())
                                {
                                    string patientID = ikouTableReader["PatientID"]?.ToString() ?? "";
                                    string orderNumber = ikouTableReader["OrderNumber"]?.ToString() ?? "";
                                    string orderNumberID = "IO" + orderNumber;

                                    List<string> missingTables = new List<string>();

                                    //PATIENTINFOチェック
                                    command_patientinfo.Parameters["id"].Value = patientID;
                                    if (command_patientinfo.ExecuteScalar() == null) missingTables.Add("PATIENTINFO");

                                    //EXAMINFOチェック
                                    command_examinfo.Parameters["id"].Value = orderNumberID;
                                    if (command_examinfo.ExecuteScalar() == null) missingTables.Add("EXAMINFO");

                                    //EXAMBUIINFOチェック
                                    command_exambuinfo.Parameters["id"].Value = orderNumberID;
                                    if (command_exambuinfo.ExecuteScalar() == null) missingTables.Add("EXAMBUIINFO");

                                    //ALERTCOMMENTチェック
                                    command_alertcomment.Parameters["id"].Value = orderNumberID;
                                    if (command_alertcomment.ExecuteScalar() == null) missingTables.Add("ALERTCOMMENT");

                                    //REPORTINFOチェック
                                    command_reportinfo.Parameters["id"].Value = orderNumberID;
                                    if (command_reportinfo.ExecuteScalar() == null) missingTables.Add("REPORTINFO");

                                    //IMAGEINFOチェック
                                    for (int i = 1; i <= 20; i++)
                                    {
                                        string keyNum = ikouTableReader[$"Key_Number_{i}"]?.ToString();
                                        if (!string.IsNullOrEmpty(keyNum))
                                        {
                                            command_imageinfo.Parameters["id"].Value = orderNumberID;
                                            command_imageinfo.Parameters["showOrder"].Value = keyNum;
                                            if (command_imageinfo.ExecuteScalar() == null)
                                            {
                                                missingTables.Add($"IMAGEINFO(Key_{i})");
                                            }
                                        }
                                    }

                                    string resultStatus = "OK";
                                    string resultText = "移行結果OK";

                                    if (missingTables.Count > 0)
                                    {
                                        errorCount++;
                                        resultStatus = "NG";
                                        resultText = "移行結果NG: " + string.Join(", ", missingTables);
                                        AppendLog($"OrderNumber={orderNumber}, PatientID={patientID}, {resultText}", "WARN");
                                    }
                                    else
                                    {
                                        passedCount++;
                                    }

                                    // Update result
                                    command_ikou_update.Parameters["ikou_result"].Value = resultStatus;
                                    command_ikou_update.Parameters["ikou_text"].Value = resultText;
                                    command_ikou_update.Parameters["orderNumber"].Value = orderNumber;
                                    command_ikou_update.ExecuteNonQuery();
                                }

                                AppendLog($"移行結果チェック完了: OK: {passedCount}件, NG: {errorCount}件", errorCount > 0 ? "WARN" : "INFO");
                                MessageBox.Show($"移行結果チェック完了。\nOK: {passedCount}件, NG: {errorCount}件");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"移行結果チェック中にエラーが発生しました: {ex.Message}", "ERROR");
                MessageBox.Show($"移行結果チェック中にエラーが発生しました: {ex.Message}");
            }
        }



        //移行実施ボタン
        private void execButton_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime fromDate = dateTimePickerFrom.Value.Date;
                DateTime toDate = dateTimePickerTo.Value.Date.AddDays(1); // Next day midnight for inclusive range

                string whereClause;
                if (dateMode == "取込日")
                {
                    whereClause = "torikomi_date >= :fromDate AND torikomi_date < :toDate";
                }
                else
                {
                    whereClause = "TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') >= :fromDate AND TO_DATE(StudyDateTime, 'YYYY/MM/DD HH24:MI:SS') < :toDate";
                }

                AppendLog($"移行開始：{dateMode}：{fromDate}～{toDate}", "INFO");

                string connString = $"User Id={dbUserId};Password={dbPassword};Data Source={dbSource};";

                using (OracleConnection conn = new OracleConnection(connString))
                {
                    conn.Open();

                    //対象内のOrderNumberを探す
                    List<string> orderList = new List<string>();
                    string selectSql = $"SELECT OrderNumber FROM IKOU.IKOU_TEMP_TABLE WHERE {whereClause}";

                    using (OracleCommand cmd = new OracleCommand(selectSql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate;
                        cmd.Parameters.Add("toDate", OracleDbType.Date).Value = toDate;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                orderList.Add(reader["OrderNumber"]?.ToString() ?? "");
                            }
                        }
                    }

                    AppendLog($"移行件数：{orderList.Count}件。処理を開始します...", "INFO");

                    //ログ用カウンター
                    int totalSuccessCount = 0;
                    int totalFailCount = 0;

                    int patientInfoOK = 0;
                    int patientInfoNG = 0;
                    int examInfoOK = 0;
                    int examInfoNG = 0;
                    int examBuiInfoOK = 0;
                    int examBuiInfoNG = 0;
                    int reportInfoOK = 0;
                    int reportInfoNG = 0;
                    int alertCommentOK = 0;
                    int alertCommentNG = 0;
                    int imageInfoOK = 0;
                    int imageInfoNG = 0;

                    //OrderNumberをloopして、移行を起こらう
                    foreach (string orderNum in orderList)
                    {
                        if (string.IsNullOrEmpty(orderNum)) continue;

                        using (OracleTransaction trans = conn.BeginTransaction())
                        {

                            //最後にログ用(OK/NGカウンター)
                            bool orderError = false;



                            // PATIENTINFO
                            try
                            {
                                string sqlPatient = @"
                                    MERGE INTO MRMS.PATIENTINFO target
                                    USING (
                                        SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                        WHERE OrderNumber = :orderNumber
                                    ) source
                                    ON (target.ID = source.PatientID)
                                    WHEN MATCHED THEN
                                        UPDATE SET
                                            target.WARD = source.HospitalWard,
                                            target.ROMA = source.PatientName,
                                            target.KANA = source.PatientKana,
                                            target.KANJI = source.PatientKanji,
                                            target.SEX = CASE source.PatientSex WHEN '1' THEN 'M' WHEN '2' THEN 'F' ELSE '' END,
                                            target.BIRTHDAY = TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD'),
                                            target.INOUTPATIENT = source.PatientStatus,
                                            target.ATTRIBUTE = 1
                                    WHEN NOT MATCHED THEN
                                        INSERT (WARD, ID, ROMA, KANA, KANJI, SEX, BIRTHDAY, INOUTPATIENT, ATTRIBUTE)
                                        VALUES (
                                            source.HospitalWard,
                                            source.PatientID,
                                            source.PatientName,
                                            source.PatientKana,
                                            source.PatientKanji,
                                            CASE source.PatientSex WHEN '1' THEN 'M' WHEN '2' THEN 'F' ELSE '' END,
                                            TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD'), source.PatientStatus,
                                            1)";

                                using (OracleCommand cmd = new OracleCommand(sqlPatient, conn))
                                {
                                    cmd.Transaction = trans;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                    cmd.ExecuteNonQuery();
                                }
                                patientInfoOK++;
                            }
                            catch (Exception ex)
                            {
                                patientInfoNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} PATIENTINFO エラー: {ex.Message}", "ERROR");
                            }




                            // EXAMINFO
                            try
                            {
                                string sqlExam = @"
                                    MERGE INTO MRMS.EXAMINFO target
                                    USING (
                                        SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                        WHERE OrderNumber = :orderNumber
                                    ) source
                                    ON (target.ID = 'IO' || source.OrderNumber)
                                    WHEN MATCHED THEN
                                        UPDATE SET
                                            target.RPTID = 'IO' || source.OrderNumber,
                                            target.ACNO = source.AccessionNumber,
                                            target.ODRID = source.AccessionNumber,
                                            target.STUDYINSTANCEUID = source.StudyinstanceUID,
                                            target.REQUESTDATE = TO_DATE(source.OrderDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            target.REQUESTSECTION = source.ReferringDepartmentName,
                                            target.REQUESTDOCTOR = source.ReferringPhysician,
                                            target.DIAGNOSIS = source.ClinicalInformation,
                                            target.PURPOSE = source.StudyInformation,
                                            target.EXAMDATE = TO_DATE(source.StudyPointTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            target.MODALITY = source.Modality,
                                            target.DETAILLOCUS = ',' || source.BodyPart || ',',
                                            target.LOCUS = ',' || source.BodyPart || ',',
                                            target.WARD = source.HospitalWard,
                                            target.ENFORCETC = source.OperatorName,
                                            target.REMARKS = source.OperatorComment,
                                            target.PATID = source.PatientID,
                                            target.ROMA = source.PatientName,
                                            target.KANA = source.PatientKana,
                                            target.KANJI = source.PatientKanji,
                                            target.SEX = CASE source.PatientSex WHEN '1' THEN 'M' WHEN '2' THEN 'F' ELSE '' END,
                                            target.BIRTHDAY = TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD'),
                                            target.INOUTPATIENT = source.PatientStatus,
                                            target.SPARE_D1 = TO_DATE(source.StudyDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            target.KENSAHOUHOU = source.StudyDescription,
                                            target.AGE = TRUNC(MONTHS_BETWEEN(TO_DATE(source.StudyPointTime, 'YYYY/MM/DD HH24:MI:SS'), TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD')) / 12),
                                            target.HSPID = 1,
                                            target.STATUS = CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END,
                                            target.CALCULATEFLAG = 0,
                                            target.DISPREMOTESTATUS = -1,
                                            target.ALLOCATIONOMITFLAG = 0
                                    WHEN NOT MATCHED THEN
                                        INSERT (ID, RPTID, ACNO, ODRID, STUDYINSTANCEUID, REQUESTDATE, REQUESTSECTION, REQUESTDOCTOR, DIAGNOSIS, PURPOSE, EXAMDATE, MODALITY, DETAILLOCUS, LOCUS, WARD, ENFORCETC, REMARKS, PATID, ROMA, KANA, KANJI, SEX, BIRTHDAY, INOUTPATIENT, SPARE_D1, KENSAHOUHOU, AGE, HSPID, STATUS, CALCULATEFLAG, DISPREMOTESTATUS, ALLOCATIONOMITFLAG)
                                        VALUES (
                                            'IO' || source.OrderNumber,
                                            'IO' || source.OrderNumber,
                                            source.AccessionNumber,
                                            source.AccessionNumber,
                                            source.StudyinstanceUID,
                                            TO_DATE(source.OrderDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            source.ReferringDepartmentName,
                                            source.ReferringPhysician,
                                            source.ClinicalInformation,
                                            source.StudyInformation,
                                            TO_DATE(source.StudyPointTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            source.Modality,
                                            ',' || source.BodyPart || ',',
                                            ',' || source.BodyPart || ',',
                                            source.HospitalWard,
                                            source.OperatorName,
                                            source.OperatorComment,
                                            source.PatientID,
                                            source.PatientName,
                                            source.PatientKana,
                                            source.PatientKanji,
                                            CASE source.PatientSex WHEN '1' THEN 'M' WHEN '2' THEN 'F' ELSE '' END,
                                            TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD'),
                                            source.PatientStatus,
                                            TO_DATE(source.StudyDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            source.StudyDescription,
                                            TRUNC(MONTHS_BETWEEN(TO_DATE(source.StudyPointTime, 'YYYY/MM/DD HH24:MI:SS'), TO_DATE(source.PatientBirthDate, 'YYYY/MM/DD')) / 12),
                                            1,
                                            CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END,
                                            0,
                                            -1,
                                            0)";

                                using (OracleCommand cmd = new OracleCommand(sqlExam, conn))
                                {
                                    cmd.Transaction = trans;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                    cmd.ExecuteNonQuery();
                                }
                                examInfoOK++;
                            }
                            catch (Exception ex)
                            {
                                examInfoNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} EXAMINFO エラー: {ex.Message}", "ERROR");
                            }




                            // EXAMBUIINFO
                            try
                            {
                                string sqlBui = @"
                                    MERGE INTO MRMS.EXAMBUIINFO target
                                    USING (
                                        SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                        WHERE OrderNumber = :orderNumber
                                    ) source
                                    ON (target.ID = 'IO' || source.OrderNumber)
                                    WHEN MATCHED THEN
                                        UPDATE SET
                                            target.BUI_NAME = source.BodyPart,
                                            target.BUIBUNRUI_NAME = source.BodyPart,
                                            target.HOUHOU_NAME = source.StudyDescription,
                                            target.NO = 1
                                    WHEN NOT MATCHED THEN
                                        INSERT (ID, BUI_NAME, BUIBUNRUI_NAME, HOUHOU_NAME, NO)
                                        VALUES (
                                            'IO' || source.OrderNumber,
                                            source.BodyPart,
                                            source.BodyPart,
                                            source.StudyDescription,
                                            1)";

                                using (OracleCommand cmd = new OracleCommand(sqlBui, conn))
                                {
                                    cmd.Transaction = trans;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                    cmd.ExecuteNonQuery();
                                }
                                examBuiInfoOK++;
                            }
                            catch (Exception ex)
                            {
                                examBuiInfoNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} EXAMBUIINFO エラー: {ex.Message}", "ERROR");
                            }




                            // REPORTINFO
                            try
                            {
                                string sqlReport = @"
                                    MERGE INTO MRMS.REPORTINFO target
                                    USING (
                                        SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                        WHERE OrderNumber = :orderNumber
                                    ) source
                                    ON (target.ID = 'IO' || source.OrderNumber)
                                    WHEN MATCHED THEN
                                        UPDATE SET
                                            target.DRAWDOCTOR = '/' || source.DiagnosingUser || '/' || CASE WHEN source.RevisingUser IS NOT NULL THEN source.RevisingUser || '/' ELSE '' END,
                                            target.DRAWDATE = CASE WHEN source.RevCloseDateTime IS NOT NULL THEN TO_DATE(source.RevCloseDateTime, 'YYYY/MM/DD HH24:MI:SS') ELSE TO_DATE(source.DiagnosedDateTime, 'YYYY/MM/DD HH24:MI:SS') END,
                                            target.FIXDOCTOR = CASE WHEN source.RevisingUser <> source.ApprovingUser THEN '/' || source.RevisingUser || '/' || source.ApprovingUser || '/' ELSE '/' || source.ApprovingUser || '/' END,
                                            target.FIXDATE = TO_DATE(source.ApprovedDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            target.FINDINGS = source.Finding,
                                            target.IMPRESSION = source.Diagnosis,
                                            target.REVISION = 0,
                                            target.SUBREVISION = -1,
                                            target.STATUS = CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END
                                    WHEN NOT MATCHED THEN
                                        INSERT (ID, DRAWDOCTOR, DRAWDATE, FIXDOCTOR, FIXDATE, FINDINGS, IMPRESSION, REVISION, SUBREVISION, STATUS)
                                        VALUES (
                                            'IO' || source.OrderNumber,
                                            '/' || source.DiagnosingUser || '/' || CASE WHEN source.RevisingUser IS NOT NULL THEN source.RevisingUser || '/' ELSE '' END,
                                            CASE WHEN source.RevCloseDateTime IS NOT NULL THEN TO_DATE(source.RevCloseDateTime, 'YYYY/MM/DD HH24:MI:SS') ELSE TO_DATE(source.DiagnosedDateTime, 'YYYY/MM/DD HH24:MI:SS') END,
                                            CASE WHEN source.RevisingUser <> source.ApprovingUser THEN '/' || source.RevisingUser || '/' || source.ApprovingUser || '/' ELSE '/' || source.ApprovingUser || '/' END,
                                            TO_DATE(source.ApprovedDateTime, 'YYYY/MM/DD HH24:MI:SS'),
                                            source.Finding,
                                            source.Diagnosis,
                                            0,
                                            -1,
                                            CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END)";

                                using (OracleCommand cmd = new OracleCommand(sqlReport, conn))
                                {
                                    cmd.Transaction = trans;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                    cmd.ExecuteNonQuery();
                                }
                                reportInfoOK++;
                            }
                            catch (Exception ex)
                            {
                                reportInfoNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} REPORTINFO エラー: {ex.Message}", "ERROR");
                            }




                            // ALERTCOMMENT
                            try
                            {
                                string sqlAlert = @"
                                    MERGE INTO MRMS.ALERTCOMMENT target
                                    USING (
                                        SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                        WHERE OrderNumber = :orderNumber
                                    ) source
                                    ON (target.ID = 'IO' || source.OrderNumber)
                                    WHEN MATCHED THEN
                                        UPDATE SET
                                            target.CONTENTS = source.Conclusion
                                    WHEN NOT MATCHED THEN
                                        INSERT (ID, CONTENTS)
                                        VALUES (
                                            'IO' || source.OrderNumber,
                                            source.Conclusion)";

                                using (OracleCommand cmd = new OracleCommand(sqlAlert, conn))
                                {
                                    cmd.Transaction = trans;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                    cmd.ExecuteNonQuery();
                                }
                                alertCommentOK++;
                            }
                            catch (Exception ex)
                            {
                                alertCommentNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} ALERTCOMMENT エラー: {ex.Message}", "ERROR");
                            }




                            // IMAGEINFO
                            try
                            {
                                for (int i = 1; i <= 20; i++)
                                {
                                    string keyNumberCol = $"Key_Number_{i}";
                                    string keyTitleCol = $"Key_Title_{i}";
                                    string keyFileCol = $"Key_File_{i}";

                                    string sqlImage = $@"
                                        MERGE INTO MRMS.IMAGEINFO target
                                        USING (
                                            SELECT * FROM IKOU.IKOU_TEMP_TABLE
                                            WHERE OrderNumber = :orderNumber AND {keyNumberCol} IS NOT NULL AND {keyFileCol} IS NOT NULL
                                        ) source
                                        ON (target.ID = 'IO' || source.OrderNumber AND target.SHOWORDER = source.{keyNumberCol})
                                        WHEN MATCHED THEN
                                            UPDATE SET
                                                target.FILENAME = source.{keyFileCol},
                                                target.IMAGECOMMENT = source.{keyTitleCol},
                                                target.REVISION = 0,
                                                target.SUBREVISION = -1,
                                                target.STATUS =  CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END,
                                                target.FILEPATH = '{keyImagePath}'||'\'||TO_CHAR(TO_DATE(source.StudyDateTime, 'YYYY/MM/DD HH24:MI:SS'), 'YYYYMM') || '\' || source.OrderNumber  || '\',
                                                target.OTHERSTUDY = -1,
                                                target.TEMPLATEID = -1,
                                                target.TEMPLATEIMAGENO = -1,
                                                target.DTGUID = TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS') || 'IO' || source.OrderNumber || source.{keyNumberCol}
                                        WHEN NOT MATCHED THEN
                                            INSERT (ID, SHOWORDER, FILENAME, IMAGECOMMENT, REVISION, SUBREVISION, STATUS, FILEPATH, OTHERSTUDY, TEMPLATEID, TEMPLATEIMAGENO, DTGUID)
                                            VALUES (
                                                'IO' || source.OrderNumber,
                                                source.{keyNumberCol},
                                                source.{keyFileCol},
                                                source.{keyTitleCol},
                                                0,
                                                -1,
                                                CASE WHEN source.ApprovingUser IS NULL OR source.ApprovedDateTime IS NULL THEN 70 ELSE 90 END,
                                                '{keyImagePath}'||'\'||TO_CHAR(TO_DATE(source.StudyDateTime, 'YYYY/MM/DD HH24:MI:SS'), 'YYYYMM') || '\' || source.OrderNumber  || '\',
                                                -1,
                                                -1,
                                                -1,
                                                TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS') || 'IO' || source.OrderNumber || source.{keyNumberCol}
                                            )";

                                    using (OracleCommand cmd = new OracleCommand(sqlImage, conn))
                                    {
                                        cmd.Transaction = trans;
                                        cmd.BindByName = true;
                                        cmd.Parameters.Add("orderNumber", OracleDbType.Varchar2).Value = orderNum;
                                        imageInfoOK += cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                imageInfoNG++;
                                orderError = true;
                                AppendLog($"OrderNumber {orderNum} IMAGEINFO エラー: {ex.Message}", "ERROR");
                            }

                            try
                            {
                                trans.Commit();
                                if (orderError) totalFailCount++; else totalSuccessCount++;
                            }
                            catch (Exception ex)
                            {
                                totalFailCount++;
                                AppendLog($"OrderNumber {orderNum} DB保存エラー: {ex.Message}", "ERROR");
                            }
                        }
                    }

                    AppendLog($"処理完了。OK: {totalSuccessCount}件, NG: {totalFailCount}件", "INFO");
                    AppendLog($"PATIENTINFO: OK: {patientInfoOK}件, NG: {patientInfoNG}件", "INFO");
                    AppendLog($"EXAMINFO: OK: {examInfoOK}件, NG: {examInfoNG}件", "INFO");
                    AppendLog($"EXAMBUIINFO: OK: {examBuiInfoOK}件, NG: {examBuiInfoNG}件", "INFO");
                    AppendLog($"REPORTINFO: OK: {reportInfoOK}件, NG: {reportInfoNG}件", "INFO");
                    AppendLog($"ALERTCOMMENT: OK: {alertCommentOK}件, NG: {alertCommentNG}件", "INFO");
                    AppendLog($"IMAGEINFO: OK: {imageInfoOK}件, NG: {imageInfoNG}件", "INFO");
                } // End using connection

                checkAndAppendIkouTableResult();
            }
            catch (Exception ex)
            {
                AppendLog($"移行中にエラーが発生しました：{ex.Message}", "ERROR");
                MessageBox.Show($"移行中にエラーが発生しました：{ex.Message}");
            }
        }

        //ログ出力
        private void AppendLog(string message, string level = "INFO")
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendLog(message, level)));
                return;
            }

            switch (level.ToUpper())
            {
                case "ERROR": log.Error(message); break;
                case "WARN": log.Warn(message); break;
                default: log.Info(message); break;
            }
        }

        //終了ボタン
        private void quitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}
