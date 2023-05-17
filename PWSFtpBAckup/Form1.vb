Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Text
Imports System.Threading


Public Class Form1
    Dim IsFirstPackajeForSane As Boolean = True
    Dim PWS As ParsicWebService.Service1 = New ParsicWebService.Service1
    Dim MyLabService As LabService.Service1SoapClient = New LabService.Service1SoapClient
    Public MyPublic As New Cls_Public
    Dim FtpServer As New IISFtpService.WebService1SoapClient
    Private trd1 As Thread
    Private PWSTrd As Thread
    Private PWSDiffTrd As Thread
    Private PWSDeleteTrd As Thread
    Private LabTrd As Thread
    Private Trd2 As Thread
    Private TrdSendEmail As Thread
    Dim DT As New DataTable("Dt")
    Dim Rdt As New DataTable("Repository")

    Dim _FTPServerPath As String = "ftp://81.16.116.84/" '    "ftp:/192.168.1.20/"     '  "ftp://185.189.122.57/" '  
    Dim FTPIPAddress As String = "81.16.116.84"
    Dim FTPBackupSubPath As String = "PWS_Backups/"
    Dim _FTPUsername As String = "Administrator"
    Dim _FTPPassword As String = "*****"
    Dim SizeOfEachFiles As String = "20971520"   '  "41943040"  '   "1048576"  '
    Dim HourForGetPwsBack As Int16 = 10
    Dim LabID As Int32 = 0
    Dim DifNumber As Int16 = 3
    Dim IsError As Boolean = False
    Dim FtpUrl As String = ""
    Dim BackupFinish As Boolean = False
    Dim Counter As Int16


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load


    End Sub

    Private Sub Btn_SendEmail_Click(sender As Object, e As EventArgs) Handles Btn_SendEmail.Click
        Try
            Dim SleepTime As Integer = FindTimeDifference(14, 41)
            TrdSendEmail = New Thread(AddressOf CheckBackupErrorLogs)
            TrdSendEmail.Start()
        Catch ex As Exception

        End Try
        Btn_SendEmail.Enabled = False
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Btn_Start.Click






        'Txt_DBName = TXTDb_ParsicMaster
        'Txt_Connection = server=KEYHAN\AZARJOO2014;database=Db_ParsicMaster;user id=sa;password=who;Max Pool Size=800;
        'Txt_Path = D:\Test For Backup\

        Rdt = PWS.Get_BackupRepository(MyPublic.Get_UserName(), MyPublic.Get_Password(), -1)
        '_FTPServerPath = Rdt.Rows(0)("Str_FTPPath").ToString()
        '_FTPUsername = Rdt.Rows(0)("Str_FTPUsername").ToString()
        '_FTPPassword = Rdt.Rows(0)("Str_FTPPassword").ToString()

        Dim ip As String = _FTPServerPath.Replace("ftp://", "").Replace("/", "")
        Dim URL As String = "http://" + ip + ":8595/Service.asmx"

        'Dim add As ServiceModel.EndpointAddress = New ServiceModel.EndpointAddress(URL)
        'FtpServer.Endpoint.Address = add



        PWSTrd = New Thread(AddressOf PWSFullBackup)
        PWSTrd.Start()

        PWSDiffTrd = New Thread(AddressOf PWSDiffBackup)
        PWSDiffTrd.Start()

        PWSDeleteTrd = New Thread(AddressOf PWSDeleteBackup)
        PWSDeleteTrd.Start()


        'LabTrd = New Thread(AddressOf RunLabsSchedule)
        'LabTrd.Start()

        Btn_Start.Enabled = False
    End Sub

    Public Sub CheckBackupErrorLogs()
        While True
            'Dim SleepTime As Integer = FindTimeDifference(23, 59)
            'If SleepTime < 3 Then
            '    Thread.Sleep(30 * 1000)
            'Else
            '    Thread.Sleep((SleepTime - 2) * 60 * 1000)

            'End If
            Dim Errormessage As String = ""
            Dim ConnectionString As String = Txt_Connection.Text
            Dim FinalMessage As String = ""
            FinalMessage = GetLabsAutoBackup(ConnectionString)

            Dim ans As Boolean = Send("ParsicAutoBackup@gmail.com", "Parsic123", "souroshsahragard@gmail.com", "AutoBackup Logs", FinalMessage, Errormessage)
            If ans = True Then

            Else
                'MessageBox.Show(Errormessage, "Error In Send Email")
                SaveTextExeption("خطا در ارسال ایمیل بک آپ ها" + vbCrLf + Errormessage)
            End If
            Dim H As Int16 = 23
            Dim M As Int16 = 59
            Try
                H = Txt_H.Text
                M = Txt_M.Text
            Catch ex As Exception

            End Try

            Dim SleepTime As Integer = FindTimeDifference(H, M)
            If SleepTime < 3 Then
                Thread.Sleep(30 * 1000)
            Else
                Thread.Sleep((SleepTime - 2) * 60 * 1000)
            End If
        End While
    End Sub
    Public Function FindTimeDifference(MyHour As Int32, MyMin As Int32) As Integer
        Try
            Dim Hour As Int32 = MyHour - Now.Hour
            Dim Min As Int32 = MyMin - Now.Minute
            Dim FinalMin As Integer

            If Hour < 0 Then
                Hour = (24 - Now.Hour) + MyHour
            End If


            If Min < 0 Then
                Min = (60 - Now.Minute) + MyMin
                FinalMin = ((Hour - 1) * 60) + Min
            Else
                FinalMin = (Hour * 60) + Min
            End If
            If (FinalMin < 0) Then
                FinalMin = 1440 + FinalMin
            End If
            Return FinalMin
        Catch ex As Exception
            SaveTextExeption("Error 09 : " + ex.Message.ToString())
        End Try

    End Function
    Public Function GetLabsAutoBackup(Connection As String) As String
        Dim EmailMessage As String = ""

        Threading.Thread.Sleep(250)
        Dim con As SqlConnection = New SqlConnection(Connection)
        Try
            Dim DT As New DataTable("DT")
            Dim cmd As SqlCommand = New SqlCommand("execute SP_Get_AutoBackupFingerLogs @Frk_LabID = 0, @Int_Status = 2", con)
            cmd.CommandTimeout = 900
            con.Open()
            Dim ad As SqlDataAdapter = New SqlDataAdapter(cmd)
            cmd.ExecuteNonQuery()
            ad.Fill(DT)
            con.Close()
            Dim NoBack As String = ""
            EmailMessage = "تعداد آزمایشگاه ها : " + DT.Rows.Count().ToString() + vbCrLf + vbCrLf + vbCrLf + vbCrLf
            EmailMessage = EmailMessage + "آزمایشگاه هایی که در یک هفته اخیر و در آخرین تلاش بک آپ فول و دیفرنشیال گرفته اند" + vbCrLf + vbCrLf
            For j As Int16 = 0 To DT.Rows.Count() - 1
                If (DT.Rows(j)("Bit_IsAutoBackupFullTaken") And DT.Rows(j)("Bit_IsAutoBackupDiffTaken")) Then
                    EmailMessage = EmailMessage + DT.Rows(j)("Str_LabPersianName").ToString() + vbCrLf
                Else
                    NoBack = NoBack + DT.Rows(j)("Str_LabPersianName").ToString() + vbCrLf
                End If
            Next

            EmailMessage = EmailMessage + vbCrLf + vbCrLf + vbCrLf + "آزمایشگاه هایی که در یک هفته اخیر برای بک آپ تلاش کرده اند ولی بک آپ برای فول، دیفرنشیال یا هردو گرفته نشده است و با خطا روبرو شده اند " + vbCrLf + vbCrLf
            EmailMessage = EmailMessage + NoBack

            'EmailMessage = EmailMessage + "نام آزمایشگاه                                 Full           Diff      " + vbCrLf + vbCrLf
            'Dim labname As String = ""

            'For j As Int16 = 0 To DT.Rows.Count() - 1
            '    labname = DT.Rows(j)("Str_LabPersianName").ToString()
            '    For i As Int16 = labname.Length To 60 - labname.Length
            '        labname = labname + " "
            '    Next

            '    EmailMessage += labname + "" + "     " + DT.Rows(j)("Bit_IsAutoBackupFullTaken").ToString() + "           " + DT.Rows(j)("Bit_IsAutoBackupDiffTaken").ToString() + "      " + vbCrLf + vbCrLf
            'Next





            Return EmailMessage
        Catch ex As Exception
            SaveTextExeption("اررور در انجام تسک بک آپ گیری اسکیوال " + ex.Message.ToString())
            Return "Error : " + vbCrLf + ex.Message.ToString()
            con.Close()
        End Try

        Return "Errooooorrrr"

    End Function
    Public Function Send(MyUserName As String, MyPassword As String, EmailTo As String, Subject As String, Body As String, ByRef Errormessage As String)
        Try
            Using mm As New MailMessage(MyUserName, EmailTo)
                mm.Subject = Subject
                mm.Body = Body
                'For Each filePath As String In OpenFileDialog1.FileNames
                '    If File.Exists(filePath) Then
                '        Dim fileName As String = Path.GetFileName(filePath)
                '        mm.Attachments.Add(New Attachment(filePath))
                '    End If
                'Next
                mm.IsBodyHtml = False
                Dim smtp As New SmtpClient()
                smtp.Host = "smtp.gmail.com"
                smtp.EnableSsl = True
                Dim NetworkCred As New NetworkCredential(MyUserName, MyPassword)
                smtp.UseDefaultCredentials = True
                smtp.Credentials = NetworkCred
                smtp.Port = 587
                smtp.Send(mm)
                SaveTextExeption("Email Sent")
                Return True

                'MessageBox.Show("Email sent.", "Message")
            End Using
        Catch ex As Exception
            Errormessage = ex.Message.ToString()
            'SaveTextExeption(Errormessage)
            Return False
        End Try

    End Function

    Public Function RunLabsSchedule() As Boolean
        'Counter = 0
        Dim DT2 As New DataTable("DT2")
        While True
            Try
                DT = PWS.Get_AutoBackupSchedule(MyPublic.Get_UserName, MyPublic.Get_Password, 0, "", "", 2)

                For i As Int16 = 0 To DT.Rows.Count() - 1
                    IsError = False
                    LabID = Convert.ToInt32(DT.Rows(i)("Frk_LabID"))
                    DT2 = PWS.Get_AutoBackupSchedule(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, "", "", 3)
                    Try

                        Dim DOIt As Boolean = False

                        Try
                            If (DT2.Rows.Count = 0) Then
                                DOIt = True
                            Else
                                Dim j As Int16 = 0
                                For j = 0 To DT2.Rows.Count - 1

                                    If DT2.Rows(j)("Int_Status") And DT2.Rows(j)("Str_BackupType") = "Full" Then
                                        DOIt = True
                                        Exit For
                                    End If

                                Next

                                If DOIt = False Then
                                    DOIt = True
                                    For j = 0 To DT2.Rows.Count - 1

                                        If DT2.Rows(j)("Str_ErrorLog").ToString().Contains("سرعت آپلود بسیار پایین میباشد") Or DT2.Rows(j)("Str_ErrorLog").ToString().Contains("حجم فایل بک آپ پشتیبان بیشتر از") Then
                                            DOIt = False
                                            Exit For
                                        End If

                                    Next

                                End If

                            End If
                            If DT2.Rows(0)("Str_ErrorLog").ToString().Contains("فایل بر روی شبکه قابل دست") Then
                                Try
                                    Dim MyFullDiffLabsBackupLogsID = PWS.Set_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, 0, "0", "0", 0, "0", "0", "0", "0", "0", "0", "0", 0)
                                    PWS.Edit_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, MyFullDiffLabsBackupLogsID, "", False, "", "در تلاش قبل بخاطر در دسترس نبودن فایل انتقال کنسل شده است، لطفا تنظیمات کلی آزمایشگاه برای بک آپ گیری اتوماتیک را بررسی نمایید", "ارور تنظیمات کلی", 0)
                                Catch ex As Exception
                                End Try
                                DOIt = False
                            End If
                        Catch ex As Exception

                        End Try
                        If DOIt Then
                            Dim URL As String = DT.Rows(i)("Str_ServiceURL")
                            If (PingURL(URL)) Then
                                'LabTrd = New Thread(AddressOf GetBackupOnFTP)
                                'LabTrd.Start(i)
                                GetBackupOnFTP(i)
                                If IsError = False Then
                                    Thread.Sleep(1000 * 60 * 20)
                                Else
                                    Thread.Sleep(1000 * 60 * 1)
                                End If
                            Else
                                Try
                                    Dim MyFullDiffLabsBackupLogsID = PWS.Set_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, 0, "0", "0", 0, "0", "0", "0", "0", "0", "0", "0", 0)
                                    PWS.Edit_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, MyFullDiffLabsBackupLogsID, "", False, "", "عدم ارتباط با آزمایشگاه", "از سرور بالا به آزمایشگاه پینگ گرفته نمیشود", 0)
                                Catch ex As Exception
                                End Try
                            End If
                        End If

                    Catch ex As Exception
                    End Try
                Next
            Catch ex As Exception
                SaveTextExeption("Error In Get Labs Info From Cloud")
            End Try
            Thread.Sleep(1000 * 60 * 5)

            'Counter += 1
            'If (Counter > 5) Then
            '    Counter = 0
            'End If

        End While

        Return True
    End Function

    Public Function GetBackupOnFTP(ByVal index As Int16) As String
        Try
            IsError = False
            Dim LabName As String = DT.Rows(index)("Str_LabPersianName")
            Dim LabID As Int16 = DT.Rows(index)("Frk_LabID")
            Dim URL As String = DT.Rows(index)("Str_ServiceURL")

            Dim add As ServiceModel.EndpointAddress = New ServiceModel.EndpointAddress(URL)
            MyLabService = New LabService.Service1SoapClient

            MyLabService.Endpoint.Address = add
            'Dim ans As Boolean = MyLabService.TurnTransferBakcuptoFTPOn("android", "diordna", "Full")
            'Dim ans As Boolean = MyLabService.TurnTransferBakcuptoFTPOn("android", "diordna", "Diff")
            Dim ans As Boolean
            Dim MyMessage As String = ""
            If (Counter = 0) Then
                ans = MyLabService.TurnTransferBakcuptoFTPOn("android", "diordna", "Both", "", MyMessage)
            Else
                ans = MyLabService.TurnTransferBakcuptoFTPOn("android", "diordna", "Diff", "", MyMessage)
            End If
            If ans Then
                Return "OK"
            Else
                SaveTextExeption("Error In Send Command To Lab , Answer Is False")
                Try
                    Dim MyFullDiffLabsBackupLogsID = PWS.Set_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, 0, "0", "0", 0, "0", "0", "0", "0", "0", "0", "0", 0)
                    PWS.Edit_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, MyFullDiffLabsBackupLogsID, "", False, "", "پاسخ منفی از آزمایشگاه", "سرویس آزمایشگاه برای درخواست بک آپ جواب منفی داده است", 0)

                Catch ex As Exception
                End Try
                IsError = True
                Return "Error"

            End If
        Catch ex As Exception
            SaveTextExeption("Error In Send Command To Lab : " + ex.Message.ToString())
            Try
                Dim MyFullDiffLabsBackupLogsID = PWS.Set_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, 0, "0", "0", 0, "0", "0", "0", "0", "0", "0", "", 0)
                PWS.Edit_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, MyFullDiffLabsBackupLogsID, "", False, "", "ارور در ارسال درخواست به آزمایشگاه", "این پیغام بخاطر پایین بودن ورژن میباشد", 0)
            Catch ex2 As Exception
            End Try
            IsError = True
            Return "Error 101"
        End Try

        Return ""
    End Function

    Public Function PingURL(ByVal Url As String) As Boolean
        Try
            Dim request As HttpWebRequest = TryCast(WebRequest.Create(Url), HttpWebRequest)
            request.Timeout = 15000
            Using response As HttpWebResponse = TryCast(request.GetResponse(), HttpWebResponse)
                If response.StatusCode <> HttpStatusCode.OK Then Throw New Exception("Error locating web service")
            End Using
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function
    Private Function Ping_Ip_Port(ip As String, port As Integer) As Boolean
        ' Function to open a socket to the specified port to see if it is listening
        ' Connect to socket
        Dim testSocket As New System.Net.Sockets.TcpClient()
        Try
            testSocket.Connect(ip, port)
            ' The socket is accepting connections
            testSocket.Close()
            Return True
        Catch ex As Exception
            ' The port is not accepting connections
            Return False
        End Try
        Return False
    End Function
    Public Function SendEmail(Message As String, ans As String)
        Try
            Dim Errormessage As String = ""
            Dim anse2 As Boolean = Send("ParsicAutoBackup@gmail.com", "Parsic123", "Ramin_ruthful@yahoo.com", "AutoBackup Logs", Message + vbCrLf + ans, Errormessage)
            If anse2 = False Then
                SaveTextExeption("خطا در ارسال ایمیل بک آپ ها 2" + vbCrLf + Errormessage)
            End If
            Dim anse3 As Boolean = Send("ParsicAutoBackup@gmail.com", "Parsic123", "souroshsahragard@gmail.com", "AutoBackup Logs", Message + vbCrLf + ans, Errormessage)
            If anse3 = False Then
                SaveTextExeption("خطا در ارسال ایمیل بک آپ ها 3" + vbCrLf + Errormessage)
            End If
        Catch ex As Exception

        End Try
    End Function
    Public Function PWSFullBackup() As Boolean
        Dim ConnectionString As String = Txt_Connection.Text
        Dim DbName As String = Txt_DBName.Text
        Dim Path As String = Txt_Path.Text
        While True
            Try
                Dim hour As Int16 = Date.Now().Hour

                If hour >= 1 And hour <= 5 Then
                    Dim BackupFileName As String = DbName + Date.Now.ToString().Replace("/", "").Replace(":", "") + "Full.bak"
                    Dim FilePath As String = Path + BackupFileName
                    Dim LogID As Int64 = SaveLog("شروع بک آپ گیری فول", "Full", FilePath, ConnectionString)
                    Dim info As String = "DbName : " + DbName + vbCrLf + "File Path : " + FilePath + vbCrLf + "FTP Address : " + _FTPServerPath
                    Dim ans1 As String = BackupAdvance(DbName, FilePath, ConnectionString)

                    If ans1 = "Complete" Then

                        Dim ping As Boolean = Ping_Ip_Port(FTPIPAddress, 21)
                        If ping = False Then
                            SaveTextExeption(" خطا فول :    آی پی شرکت و یا پرت 21 در دسترس نمیباشد")
                            UpdateLog(LogID, "بک آپ فول گرفته شد، آی پی شرکت و یا پرت 21  در دسترس نمیباشد", True, False, ConnectionString)
                            Thread.Sleep(1000 * 60 * 60 * 2)
                        Else
                            UpdateLog(LogID, "در حال ارسال فول به سرور شرکت", True, False, ConnectionString)
                            Dim ans As String = SplitAndSendFile(FilePath, BackupFileName, FTPBackupSubPath, _FTPServerPath, _FTPUsername, _FTPPassword, SizeOfEachFiles)
                            If ans.Contains("ok") Then
                                UpdateLog(LogID, "پایان ارسال فول", True, True, ConnectionString)
                                BackupFinish = True
                                Thread.Sleep(1000 * 60 * 60 * 10)
                            Else
                                UpdateLog(LogID, "خطا در ارسال فول     " + ans, True, False, ConnectionString)
                                SendEmail("در ارسال بانک اطلاعاتی فول به سرور شرکت، مشکلی بوجود آمده است", ans)
                                Thread.Sleep(1000 * 60 * 60 * 1)
                            End If
                        End If


                    Else
                        UpdateLog(LogID, "خطا در بک آپ گیری فول     " + ans1, False, False, ConnectionString)
                        SendEmail("در گرفتن بک آپ فول در سرور ابری خطایی رخ داده است و بک آپ گرفته نشد", ans1)
                    End If
                Else
                    Thread.Sleep(1000 * 60 * 60 * 1)
                End If


                'Thread.Sleep(Convert.ToInt64(Txt_Hour.Text) * 1000 * 60 * 60)
                'Thread.Sleep(Convert.ToInt64(Txt_Hour.Text) * 1000 * 60)
            Catch ex As Exception
                SaveTextExeption("Error In Get Full BackUp (10) : " + ex.Message.ToString())
                'Thread.Sleep(Convert.ToInt64(Txt_Hour.Text) * 1000 * 60 * 60)
                'Thread.Sleep(Convert.ToInt64(Txt_Hour.Text) * 1000 * 60)

            End Try
            'PWSDiffTrd.Interrupt()
            IsFirstPackajeForSane = True
        End While

        Return True
    End Function

    Public Function PWSDiffBackup() As Boolean
        'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
        While True
            Try
                Dim ConnectionString As String = Txt_Connection.Text

                Dim DbName As String = Txt_DBName.Text
                Dim Path As String = Txt_Path.Text
                Dim BackupFileName As String = DbName + Date.Now.ToString().Replace("/", "").Replace(":", "") + "Diff.bak"
                Dim FilePath As String = Path + BackupFileName
                Dim LogID As Int64 = SaveLog("شروع بک آپ گیری دیف", "Diff", FilePath, ConnectionString)
                Dim info As String = "DbName : " + DbName + vbCrLf + "File Path : " + FilePath + vbCrLf + "FTP Address : " + _FTPServerPath
                Dim ans1 As String = DiffAdvance(DbName, FilePath, ConnectionString)
                If ans1 = "Complete" Then
                    UpdateLog(LogID, " بک آپ دیف گرفته شد", True, False, ConnectionString)

                    DifNumber = DifNumber + 1
                    Dim hour As Int16 = Date.Now().Hour
                    If (hour < 16) Then
                        If DifNumber >= 4 Then


                            'Dim ping As Boolean = Ping_Ip_Port(FTPIPAddress, 21)
                            Dim ping As Boolean = Ping_Ip_Port(FTPIPAddress, 21)

                            If ping = False Then
                                SaveTextExeption(" خطا دیف  :    آی پی شرکت و یا پرت 21 در دسترس نمیباشد")
                                UpdateLog(LogID, "بک آپ دیف گرفته شد، آی پی شرکت و یا پرت 21  در دسترس نمیباشد ", True, False, ConnectionString)
                            Else
                                UpdateLog(LogID, " در حال ارسال دیف به سرور شرکت", True, False, ConnectionString)
                                Dim ans As String = SplitAndSendFile(FilePath, BackupFileName, FTPBackupSubPath, _FTPServerPath, _FTPUsername, _FTPPassword, SizeOfEachFiles)
                                'MessageBox.Show(ans)
                                If ans.Contains("ok") Then
                                    UpdateLog(LogID, "پایان ارسال دیف", True, True, ConnectionString)
                                    BackupFinish = True
                                Else
                                    UpdateLog(LogID, " خطا در ارسال دیف    " + ans, True, False, ConnectionString)
                                End If

                            End If
                            DifNumber = 0
                        End If
                    End If

                Else
                    UpdateLog(LogID, "خطا در بک آپ گیری دیف     " + ans1, False, False, ConnectionString)
                End If



                'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
                Thread.Sleep(15 * 60 * 1000)
            Catch ex As Exception
                SaveTextExeption("Error In Get diff BackUp (11) : " + ex.Message.ToString())
                'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
                Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60)
                DifNumber = 0
            End Try
            IsFirstPackajeForSane = True
        End While
        'PWSDiffTrd.Interrupt()
        Return True
    End Function


    Public Function PWSDeleteBackup() As Boolean
        'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
        SaveTextExeption("شروع پاک کننده بک آپ ها")
        While True


            Try
                Dim ConnectionString As String = Txt_Connection.Text
                Dim dt As New DataTable("dt")
                Dim BackCount As Int16 = 0
                Dim DiffCount As Int16 = 0
                dt = GetLog(ConnectionString)
                Dim i As Int16 = 0
                For i = 0 To dt.Rows.Count() - 1
                    If dt.Rows(i)("Bit_Status") = True And dt.Rows(i)("Str_Type") = "Full" Then
                        BackCount += 1
                    End If
                    If dt.Rows(i)("Bit_Status") = True And dt.Rows(i)("Str_Type") = "Diff" Then
                        DiffCount += 1
                    End If
                    If BackCount > 3 Then
                        If dt.Rows(i)("Str_Type") = "Full" Then
                            Try
                                Dim Path As String = ""
                                Path = dt.Rows(i)("Str_FilePath")
                                File.Delete(Path)
                                DeleteLogon(dt.Rows(i)("Prk_PwsAutoBackupLog_AutoID"), ConnectionString)
                            Catch ex As Exception
                                SaveTextExeption("اررور در پاک کردن بک آپ فول، آدرس : " + dt.Rows(i)("Str_FilePath") + vbCrLf + ex.Message.ToString())
                            End Try

                        End If
                    End If
                    If DiffCount > 3 Then
                        If dt.Rows(i)("Str_Type") = "Diff" Then
                            Try
                                Dim Path As String = ""
                                Path = dt.Rows(i)("Str_FilePath")
                                File.Delete(Path)
                                DeleteLogon(dt.Rows(i)("Prk_PwsAutoBackupLog_AutoID"), ConnectionString)
                            Catch ex As Exception
                                SaveTextExeption("اررور در پاک کردن بک آپ دیف، آدرس : " + dt.Rows(i)("Str_FilePath") + vbCrLf + ex.Message.ToString())
                            End Try

                        End If
                    End If

                Next


                'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
                Thread.Sleep(10 * 1000 * 60)
            Catch ex As Exception
                SaveTextExeption("Error In Clear BackUp (11) : " + ex.Message.ToString())
                'Thread.Sleep(Convert.ToInt64(Txt_DiffHure.Text) * 1000 * 60 * 60)
                Thread.Sleep(10 * 1000 * 60)
            End Try




            Try

                Dim ConnectionString As String = Txt_Connection.Text
                Dim dt As New DataTable("dt")
                Dim BackCount As Int16 = 0
                Dim DiffCount As Int16 = 0
                dt = GetFtpSendLog(ConnectionString)
                Dim i As Int16 = 0
                For i = 0 To dt.Rows.Count() - 1
                    If dt.Rows(i)("Str_Type") = "Full" Then
                        BackCount += 1
                    End If
                    If dt.Rows(i)("Str_Type") = "Diff" Then
                        DiffCount += 1
                    End If
                    If BackCount > 3 Then
                        If dt.Rows(i)("Str_Type") = "Full" Then
                            Try
                                Dim Path As String = ""
                                Path = "C:\BACKUP_For_Parsic_FTP\parsipol_developing09082021 135443Diff.bak" ' dt.Rows(i)("Str_FilePath")
                                DeleteFileInFTP(Path)
                            Catch ex As Exception
                                SaveTextExeption("اررور در پاک کردن بک آپ فول، آدرس : " + dt.Rows(i)("Str_FilePath") + vbCrLf + ex.Message.ToString())
                            End Try

                        End If
                    End If
                    If DiffCount > 3 Then
                        If dt.Rows(i)("Str_Type") = "Diff" Then
                            Try
                                Dim Path As String = ""
                                Path = dt.Rows(i)("Str_FilePath")
                                File.Delete(Path)
                                DeleteLogon(dt.Rows(i)("Prk_PwsAutoBackupLog_AutoID"), ConnectionString)
                            Catch ex As Exception
                                SaveTextExeption("اررور در پاک کردن بک آپ دیف، آدرس : " + dt.Rows(i)("Str_FilePath") + vbCrLf + ex.Message.ToString())
                            End Try

                        End If
                    End If

                Next


            Catch ex As Exception

            End Try


        End While
        'PWSDiffTrd.Interrupt()
        Return True
    End Function


    Public Function BackupAdvance(ByVal DBList_Name As String, ByVal BackupPath As String, ByVal Connection As String) As String

        Try
            SaveTextExeption("شروع بکاپ گیری فول")

            Dim DateInfo As String = ""
            Dim LogInfo As String = ""
            Dim con As SqlConnection = New SqlConnection(Connection)

            con.Open()
            Dim com As New System.Data.SqlClient.SqlCommand("", con)
            'Dim Trs As SqlClient.SqlTransaction

            'Trs = Main_Module.db.m_con_SQL.BeginTransaction
            'com.Transaction = Trs

            '====================================================================================
            Threading.Thread.Sleep(250)
            Try
                com.CommandTimeout = 900
                com.CommandText = "backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH NOFORMAT,COMPRESSION"
                com.ExecuteNonQuery()
            Catch ex As Exception
                If ex.Message.ToString.Contains("COMPRESSION") Then
                    SaveTextExeption("اررور در بک آپ گیری فول با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())
                    com.CommandTimeout = 900
                    com.CommandText = "backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH NOFORMAT"
                    com.ExecuteNonQuery()
                Else
                    SaveTextExeption("اررور در انجام تسک بک آپ گیری فول اسکیوال " + ex.Message.ToString())
                    Return "ERROR : " + ex.Message.ToString()
                End If
            End Try
            con.Close()
            SaveTextExeption("پایان بکاپ گیری")

            Return "Complete"

        Catch ex As Exception
            SaveTextExeption("اررور در انجام تسک های اسکیوال برای بک آپ گیری فول" + vbCrLf + ex.Message.ToString())
            Throw ex
            Return "Error : " + ex.Message.ToString()
        End Try
        Return "Error"
    End Function

    Public Function DiffAdvance(ByVal DBList_Name As String, ByVal BackupPath As String, ByVal Connection As String) As String




        Try
            SaveTextExeption("شروع دیفرنشیال گیری")

            Dim DateInfo As String = ""
            Dim LogInfo As String = ""
            Dim con As SqlConnection = New SqlConnection(Connection)

            con.Open()
            Dim com As New System.Data.SqlClient.SqlCommand("", con)
            'Dim Trs As SqlClient.SqlTransaction

            'Trs = Main_Module.db.m_con_SQL.BeginTransaction
            'com.Transaction = Trs

            '====================================================================================
            Threading.Thread.Sleep(250)
            Try
                com.CommandTimeout = 900
                com.CommandText = "backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL,COMPRESSION"
                com.ExecuteNonQuery()
            Catch ex As Exception
                If ex.Message.ToString.Contains("COMPRESSION") Then
                    SaveTextExeption("Error 29 : " + vbCrLf + "اررور در بک آپ گیری دیفرنشیال با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())
                    com.CommandTimeout = 900
                    com.CommandText = "backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL"
                    com.ExecuteNonQuery()
                Else
                    SaveTextExeption("Error 30 : " + vbCrLf + "اررور در انجام تسک بک آپ گیری اسکیوال " + ex.Message.ToString())
                    Return "ERROR : " + +ex.Message.ToString()

                End If
            End Try

            con.Close()
            SaveTextExeption("پایان بکاپ گیری دیفرنشیال")

            Return "Complete"

        Catch ex As Exception
            SaveTextExeption("Error 31 :  " + vbCrLf + "اررور در انجام تسک های اسکیوال برای بک آپ گیری" + vbCrLf + ex.Message.ToString())
            Throw ex
            Return "Error : " + +ex.Message.ToString()
        End Try
        Return "Error"






        'Try
        '    'SaveTextExeption("شروع بکاپ گیری دیفرنشیال")

        '    Dim DateInfo As String = ""
        '    Dim LogInfo As String = ""

        '    '====================================================================================
        '    Threading.Thread.Sleep(250)
        '    Try
        '        _Tmpdb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL,COMPRESSION", DataAccess.DBase.ExecuteMode._NONQEURY, 900)
        '    Catch ex As Exception
        '        If ex.Message.ToString.Contains("COMPRESSION") Then
        '            SaveTextExeption("Error 29 : " + vbCrLf + "اررور در بک آپ گیری دیفرنشیال با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())

        '            _Tmpdb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL", DataAccess.DBase.ExecuteMode._NONQEURY, 900)

        '        Else
        '            SaveTextExeption("Error 30 : " + vbCrLf + "اررور در انجام تسک بک آپ گیری اسکیوال " + ex.Message.ToString())
        '            Return "Error"
        '        End If
        '    End Try

        '    'SaveTextExeption("پایان بکاپ گیری دیفرنشیال")

        '    Return "Complete"

        'Catch ex As Exception
        '    SaveTextExeption("Error 31 : " + vbCrLf + "اررور در انجام تسک های اسکیوال برای بک آپ گیری" + vbCrLf + ex.Message.ToString())
        '    Return "Error In Backup Task"
        'End Try
        'Return "Error In Backup Tasks "



    End Function

    Public Function SaveLog(ByVal Message As String, ByVal Type As String, ByVal FilePath As String, ByVal connection As String) As Int64
        Dim ID As Int64 = 0
        Dim con As SqlConnection = New SqlConnection(connection)
        con.Open()
        Dim com As New System.Data.SqlClient.SqlCommand("", con)
        Try
            com.CommandText = "execute Sp_Insert_AutoBackupLogs @Str_Message = N'" + Message + "', @Str_Type = '" + Type + "', @Str_FilePath = N'" + FilePath + "', @Bit_Status = 0, @Bit_SendToFTP = 0 "
            ID = com.ExecuteScalar()
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در ذخیره لاگ" + ex.Message.ToString())
            con.Close()
        End Try
        Return ID
    End Function
    Public Function UpdateLog(ByVal ID As Int64, ByVal Message As String, ByVal Status As Boolean, ByVal SendToFTP As Boolean, ByVal connection As String) As Int64
        Dim con As SqlConnection = New SqlConnection(connection)
        con.Open()
        Dim com As New System.Data.SqlClient.SqlCommand("", con)
        Try
            com.CommandText = "execute Sp_Update_AutoBackupLogs @Int_AutoBackupLogID = " + ID.ToString() + ", @Str_Message = N'" + Message + "', @Bit_Status = " + Status.ToString() + ", @Bit_SendToFTP = " + SendToFTP.ToString() + ""
            com.ExecuteNonQuery()
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در ذخیره لاگ" + ex.Message.ToString())
            con.Close()
        End Try

    End Function
    Public Function DeleteLogon(ByVal ID As Int64, ByVal connection As String) As Boolean
        Dim con As SqlConnection = New SqlConnection(connection)
        con.Open()
        Dim com As New System.Data.SqlClient.SqlCommand("", con)
        Try
            com.CommandText = "UPDATE Tbl_PwsAutoBackupLogs SET Bit_Delete = 1	where Prk_PwsAutoBackupLog_AutoID = " + ID.ToString()
            com.ExecuteNonQuery()
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در دیلیت لاگ" + ex.Message.ToString())
            con.Close()
        End Try

    End Function

    Public Function DeleteFileInFTP(ByVal Path As String) As Boolean

        Try
            'Dim address As String = _FTPServerPath + FTPBackupSubPath

            'Dim FileName As String = ""
            'For i As Int16 = 0 To Path.Length - 1
            '    FileName = FileName + Path(i)
            '    If Path(i) = "/" Or Path(i) = "\" Then
            '        FileName = ""
            '    End If
            'Next


            'Dim url As String = "ftp://81.16.116.84:7778/PWS_Backups/parsipol_developing09082021 20154139Diff/parsipol_developing09082021 154139Diff.bak"  'address + FileName.Replace(".bak", "") + "/" + FileName
            'DeleteFtpDirectory(Path)

            Return True
        Catch ex As Exception
            SaveTextExeption("اررور در دیلیت فایل در Ftp" + ex.Message.ToString())
        End Try

    End Function

    Public Function GetLog(ByVal connection As String) As DataTable
        Dim DT As New DataTable("dt")
        Dim con As SqlConnection = New SqlConnection(connection)
        con.Open()
        Try
            Dim Adp As New SqlClient.SqlDataAdapter("select * from Tbl_PwsAutoBackupLogs where bit_delete = 0 order by Prk_PwsAutoBackupLog_AutoID desc", con)
            Adp.Fill(DT)
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در خواندن لاگ" + ex.Message.ToString())
            con.Close()
        End Try
        Return DT
    End Function
    Public Function GetFtpSendLog(ByVal connection As String) As DataTable
        Dim DT As New DataTable("dt")
        Dim con As SqlConnection = New SqlConnection(connection)
        con.Open()
        Try
            Dim Adp As New SqlClient.SqlDataAdapter("select * from Tbl_PwsAutoBackupLogs where Bit_SendToFTP = 1 order by Prk_PwsAutoBackupLog_AutoID desc", con)
            Adp.Fill(DT)
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در خواندن لاگ" + ex.Message.ToString())
            con.Close()
        End Try
        Return DT
    End Function
    Public Function DeleteFtpDirectory(url As String)
        Try
            'Dim clsRequest As System.Net.FtpWebRequest = DirectCast(System.Net.WebRequest.Create(url), System.Net.FtpWebRequest)
            'clsRequest.Credentials = New System.Net.NetworkCredential(_FTPUsername, _FTPPassword)
            'clsRequest.Method = System.Net.WebRequestMethods.Ftp.RemoveDirectory
            'clsRequest.GetResponse()

            'Dim FTPRequest As System.Net.FtpWebRequest = DirectCast(System.Net.WebRequest.Create(url), System.Net.FtpWebRequest)
            'FTPRequest.Credentials = New System.Net.NetworkCredential(_FTPUsername, _FTPPassword)
            'FTPRequest.Method = System.Net.WebRequestMethods.Ftp.DeleteFile


            Console.WriteLine("Deleting From Server")
            Dim FTPDelReq As FileWebRequest = WebRequest.Create(url)
            FTPDelReq.Credentials = New Net.NetworkCredential(_FTPUsername, _FTPPassword)
            FTPDelReq.Method = WebRequestMethods.Ftp.DeleteFile
            Dim FTPDelResp As FileWebResponse = FTPDelReq.GetResponse

            'MessageBox.Show("File Ok", "File")

            'Dim fileName As String = "fileName"
            'Dim FtpUrl As String = "ftp://yourserver.com/"
            'Dim ftpFolder As String = "foldername/"
            'Dim FtpserverIP As String = url
            'Dim request As FtpWebRequest = CType(WebRequest.Create(FtpserverIP), FtpWebRequest)
            'request.Method = WebRequestMethods.Ftp.DeleteFile
            'request.Credentials = New NetworkCredential(_FTPUsername, _FTPPassword)
            'Dim response As FtpWebResponse = CType(request.GetResponse(), FtpWebResponse)
            ''ClientScript.RegisterClientScriptBlock(Me.[GetType](), "", "alert('" & response.StatusDescription & "')", True)
            'response.Close()


        Catch ex As Exception
            SaveTextExeption("اررور 2 در دیلیت فایل در Ftp" + ex.Message.ToString())
        End Try
    End Function



    Public Function SaveTextExeption(ByVal Text As String) As Boolean
        Try
            Text = Environment.NewLine & DateTime.Now & vbCrLf & "----------------------------------------------------------------" & vbCrLf & Text & vbCrLf & vbCrLf
            File.AppendAllText("C:\ParsicWebTemp\AutoBackupErrorLog.txt", Text & Environment.NewLine)
            Return True
        Catch EX As Exception
            Return False
        End Try
    End Function
    Public Function SaveTextExeptionAppendEnd(ByVal Text As String) As Boolean
        Try
            'Text = DateTime.Now & vbCrLf & "----------------------------------------------------------------" & vbCrLf & Text & vbCrLf & vbCrLf
            File.AppendAllText("C:\ParsicWebTemp\AutoBackupErrorLog.txt", Text & "  ")
            Return True
        Catch EX As Exception
            Return False
        End Try
    End Function


    Public Function SplitAndSendFile(ByVal filepath As String, ByVal BackUpName As String, ByVal FtpLocalPath As String, ByVal _FTPServerPath As String, ByVal _FTPUsername As String, ByVal _FTPPassword As String, Optional ByVal SizeOfEachFiles As Long = 5242880) As String
        Try
            SaveTextExeption("شروع به آپلود فایل ")
            FtpLocalPath = FtpLocalPath + BackUpName.Replace(".bak", "/")
            Dim NameOfSplite As String = "Split"
            Dim counter As Int16 = 1
            Dim ErrorCloudCount As Int32 = 0
            Dim _ans As String
            Dim size As Long = SizeOfEachFiles
            FtpServer.Endpoint.Binding.CloseTimeout = New TimeSpan(0, 5, 0)
            FtpServer.Endpoint.Binding.ReceiveTimeout = New TimeSpan(0, 5, 0)
            FtpServer.Endpoint.Binding.OpenTimeout = New TimeSpan(0, 5, 0)
            FtpServer.Endpoint.Binding.SendTimeout = New TimeSpan(0, 5, 0)
            Do
                _ans = SplitFile(filepath, size, _FTPServerPath, _FTPUsername, _FTPPassword, FtpLocalPath, NameOfSplite)
                Txt_RootPath.Text = "E:\PWS_Backups\"
                Txt_FtpLoclPath.Text = FtpLocalPath.Replace("/", "\")
                Txt_BackupName.Text = BackUpName
                Txt_SplitName.Text = "Split"
                SaveTextExeption("Txt_Assembler Info : Txt_RootPath.Text = E:\PWS_Backups\     Txt_FtpLoclPath.Text = " + FtpLocalPath.Replace("/", "\") + "     Txt_BackupName.Text = " + BackUpName + "     Txt_SplitName.Text = Split")

                If _ans = "" Then
                    Return "تلاش فراوان و عدم ارسال"
                End If
                If _ans.Contains("Ok") Then
                    'assembler
                    Dim AssemblerErrorLog As String = ""
                    Try
                        Dim ping As Boolean = Ping_Ip_Port(FTPIPAddress, 8595)
                        If ping = False Then
                            SaveTextExeption(" هشدار  :    آی پی شرکت و یا پرت 8595 برای اجرای اسمبلر در دسترس نمیباشد")
                            Exit Do
                        End If
                        Dim Assemblerans As Boolean = FtpServer.Assembling("E:\PWS_Backups\", FtpLocalPath.Replace("/", "\"), BackUpName, "Split", AssemblerErrorLog)
                        'Dim sss As String = AssemblerErrorLog 'FtpServer.assembler(FtpLocalPath, BackUpName.Replace(".bak", "/"), BackUpName, "Split")
                        If Assemblerans = True Then
                            SaveTextExeption("عملیات با موفقیت به پایان رسید")
                            Return "ok"
                        Else
                            Txt_RootPath.Text = "E:\PWS_Backups\"
                            Txt_FtpLoclPath.Text = FtpLocalPath.Replace("/", "\")
                            Txt_BackupName.Text = BackUpName
                            Txt_SplitName.Text = "Split"
                            SaveTextExeption("فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است" + vbCrLf + "Error : " + AssemblerErrorLog)
                            Return "فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است" + vbCrLf + "ERROR : " + AssemblerErrorLog
                        End If
                    Catch ex As Exception
                        Txt_RootPath.Text = "E:\PWS_Backups\"
                        Txt_FtpLoclPath.Text = FtpLocalPath.Replace("/", "\")
                        Txt_BackupName.Text = BackUpName
                        Txt_SplitName.Text = "Split"
                        SaveTextExeption("فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است، لطفا ارتباط با اینترنت یا(آی آی اس) سرور اف تی پی در شرکت را بررسی نمایید. همچنین این خطا ممکن است به علت بزرگ بودن فایل رخ داده باشد" + vbCrLf + "Error : " + ex.Message.ToString())
                        Return "فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است، لطفا ارتباط با اینترنت یا(آی آی اس) سرور اف تی پی در شرکت را بررسی نمایید" + vbCrLf + "ERROR : " + ex.Message.ToString()
                    End Try
                    'assembler\
                ElseIf _ans = "ErrorSize" Then
                    SaveTextExeption("اررور در سرعت" + size.ToString() + "در حال تلاش با سرعت" + (size / 2).ToString())
                    size = size / 2
                    If size < 1048576 Then
                        Return "Error : سرعت آپلود بسیار پایین میباشد " + vbCrLf + "Last Spead : " + (size * 2).ToString()
                        Exit Do
                    End If
                ElseIf _ans = "در ارسال لاگ به سرور ابری مشکلی وجود دارد" Then
                    ErrorCloudCount += 1
                    If ErrorCloudCount = 10 Then
                        Exit Do
                    End If
                Else

                    Return _ans
                End If
            Loop
            Return _ans
            'MergeFiles(LocalPath, "Splite*", "D:\Keyhan\Test Split Larg File\New folder\aaa.rar")

        Catch ex As Exception
            SaveTextExeption("Error 45 : In SplitAndSendFile Function " + vbCrLf + ex.Message.ToString())
            Return "Error In SplitAndSendFile Function " + vbCrLf + ex.Message.ToString()
        End Try

    End Function


    Private Function SplitFile(ByVal inputFileName As String, ByVal SizeOfEachFiles As Long, ByVal _FTPServerPath As String, ByVal _FTPUsername As String, ByVal _FTPPassword As String, ByVal FtpLocalPath As String, ByVal NameOfSplite As String) As String
        Dim returnList As New List(Of String)
        Dim fileCount As Integer = 1
        Dim ErrorFinder As Int16 = 0

        Try
            'Dim outputFileExtension As String = IO.Path.GetExtension(outputFileName)
            'outputFileName = outputFileName.Replace(outputFileExtension, "")

            Dim sr As New IO.StreamReader(inputFileName)
            Dim fileLength As Long = sr.BaseStream.Length
            Dim numberOfFiles As Long = (fileLength \ SizeOfEachFiles) + 1
            Dim baseBufferSize As Integer = CInt(fileLength \ numberOfFiles)
            Dim finished As Boolean = False

            SaveTextExeption("تعداد کل بسته ها :  " + numberOfFiles.ToString())

            ErrorFinder = 1

            Do Until finished

                Dim bufferSize As Integer = baseBufferSize
                Dim originalPosition As Long = sr.BaseStream.Position
                'find line first line feed after the base buffer length
                sr.BaseStream.Position += bufferSize
                If sr.BaseStream.Position < fileLength Then
                    Do Until sr.Read = 10
                        bufferSize += 1
                    Loop
                    bufferSize += 1
                Else
                    bufferSize = CInt(fileLength - originalPosition)
                    finished = True
                End If
                'write the chunk of data to a buffer in memory
                sr.BaseStream.Position = originalPosition
                Dim buffer(bufferSize - 1) As Byte

                sr.BaseStream.Read(buffer, 0, bufferSize)

                'write the chunk of data to a file
                'Dim outputPath As String = outputFileName & fileCount.ToString & outputFileExtension
                'returnList.Add(outputPath)


                'My.Computer.FileSystem.WriteAllBytes(outputPath, buffer, False)
                Dim myans As Boolean
                Dim ErrInfo As String = ""
                Dim errorcounter As Int16 = 0
                While (True)
                    ErrInfo = ""
                    If (fileCount = 1) Then
                        SaveTextExeption("ارسال اولین بسته ")

                    ElseIf (fileCount = 2) Then
                        SaveTextExeption("ارسال دومین بسته ")
                    ElseIf (fileCount = numberOfFiles) Then
                        SaveTextExeption("ارسال آخرین بسته، شماره بسته : " + fileCount.ToString())

                    ElseIf (fileCount > 2) Then
                        SaveTextExeptionAppendEnd(fileCount.ToString())

                    End If

                    myans = SendPackage(buffer, _FTPServerPath, _FTPUsername, _FTPPassword, FtpLocalPath, NameOfSplite + fileCount.ToString() + ".bak", ErrInfo)

                    If myans Then
                        'SaveTextExeption("ارسال بسته شماره " + fileCount.ToString())
                        Exit While
                    End If
                    SaveTextExeption("اررور در ارسال بسته شماره " + fileCount.ToString()) ' + vbCrLf + "Error Message : " + ErrInfo)

                    If ErrInfo = "ErrorSize" Then
                        Return "ErrorSize"
                    End If

                    Thread.Sleep(1000 * 10)
                    If (errorcounter >= 10) Then
                        SaveTextExeption("در ارسال اطلاعات خطایی رخ داده است و بیش از 10 بار تلاش ناموفق وجود دارد" + vbCrLf + "Error In Send File " + NameOfSplite + fileCount.ToString() + ".bak")
                        Return ""
                    End If
                    errorcounter += 1
                End While

                fileCount += 1
            Loop
        Catch ex As Exception
            If ErrorFinder = 0 Then
                SaveTextExeption("Error 46 : " + vbCrLf + "فایل بر روی شبکه قابل دست یابی نمیباشد " + vbCrLf + inputFileName + vbCrLf + ex.Message.ToString())
                Return "فایل بر روی شبکه قابل دست یابی نمیباشد " + vbCrLf + inputFileName + vbCrLf + ex.Message.ToString()
            ElseIf ErrorFinder = 1 Then
                SaveTextExeption("Error 47 : " + vbCrLf + "در خواندن فایل بر روی سرور اسکیوال خطایی رخ داده است ارسال اطلاعات به اف تی پی سرور انجام نشد است " + vbCrLf + FtpLocalPath + NameOfSplite + fileCount.ToString() + ".bak" + vbCrLf + ex.Message.ToString() + vbCrLf + inputFileName)
                Return "در خواندن فایل بر روی سرور اسکیوال خطایی رخ داده است ارسال اطلاعات به اف تی پی سرور انجام نشد است " + vbCrLf + FtpLocalPath + NameOfSplite + fileCount.ToString() + ".bak" + vbCrLf + ex.Message.ToString() + vbCrLf + inputFileName
            End If
        End Try
        Return "ارسال با موفقیت به اتمام رسید" + vbCrLf + "Ok"
    End Function



    Public Function SendPackage(ByVal Info As Byte(), ByVal FTPServerPath As String, ByVal FTPServerUsername As String, ByVal FTPServerPassword As String, ByVal _FTPBackupSubPath As String, ByVal BackupFileName As String, ByRef ErrorInfo As String) As Boolean
        Try

            'FtpServer.Timeout = 1000 * 60
            'Dim Myerror As String = FtpServer.SaveSpleat("D:\Parsic_FTP\PWS_Backups\" + BackupFileName, Info)






            Dim err As Exception
            Return UploadFTPByte(FTPServerPath, FTPServerUsername, FTPServerPassword, Info, _FTPBackupSubPath + BackupFileName, err, ErrorInfo)

            'Dim p As New Parameter
            'p.Info = Info
            'p.FTPServerPath = FTPServerPath

            'trd1 = New Thread(AddressOf sendPackageT)
            'trd1.Start(p)

            'Thread.Sleep(1000 * 20)



        Catch ex As Exception
            SaveTextExeption("Error 48 : In transfer : " + ex.Message.ToString())
            Return False
        End Try
        Return False
    End Function

    Public Function UploadFTPByte(ftpAddress As String, ftpUser As String, ftpPassword As String, buffer As Byte(), targetFileName As String, ExceptionInfo As Exception, ByRef ErrorInfo As String) As Boolean

        'SaveTextExeption("ftpAddress : " + ftpAddress + "     ftpUser : " + ftpUser + "     ftpPassword : " + ftpPassword + "     targetFileName : " + targetFileName + "     ")


        'SaveTextExeption("در حال ارسال ...   " + targetFileName)
        'Dim credential As NetworkCredential
        Dim sFtpFile As String = ""
        Dim clsStream As System.IO.Stream
        ErrorInfo = ""
        Try
            sFtpFile = ftpAddress & targetFileName ' & fileToUpload
            Dim ftpAdd As String = ftpAddress & targetFileName
            Dim clsRequest As System.Net.FtpWebRequest = DirectCast(System.Net.WebRequest.Create(ftpAdd), System.Net.FtpWebRequest)
            clsRequest.Credentials = New System.Net.NetworkCredential(ftpUser, ftpPassword)
            clsRequest.Method = System.Net.WebRequestMethods.Ftp.UploadFile
            ' read in file...
            Dim bFile() As Byte = buffer
            ' upload file...
            clsStream = clsRequest.GetRequestStream()
            clsStream.Write(bFile, 0, bFile.Length)
            clsStream.Close()
            clsStream.Dispose()
            'credential = New NetworkCredential(ftpUser, ftpPassword)
            'If ftpAddress.EndsWith("/") = False Then ftpAddress = ftpAddress & targetFileName & "/"
            'sFtpFile = ftpAddress & targetFileName ' & fileToUpload
            'Dim request1 As FtpWebRequest = DirectCast(WebRequest.Create(sFtpFile), FtpWebRequest)
            'Dim request As FtpWebRequest = WebRequest.Create(sFtpFile)
            'request.KeepAlive = False
            'request.Method = WebRequestMethods.Ftp.UploadFile
            'request.Credentials = credential
            'request.UsePassive = False
            'request.Timeout = (60 * 1000) * 3 '3 mins
            'request.ContentLength = buffer.Length
            'Dim stream As Stream = request.GetRequestStream
            'stream.Write(buffer, 0, buffer.Length)
            'stream.Close()
            'Using response As FtpWebResponse = DirectCast(request.GetResponse, FtpWebResponse)
            '    response.Close()
            'End Using
            'SaveTextExeption("ارسال شد")
            Return True
        Catch ex As Exception
            Try
                clsStream.Close()
                clsStream.Dispose()
            Catch Tex As Exception

            End Try
            SaveTextExeption("ERRRRRRRRRRor : " + ex.Message.ToString())
            If (IsFirstPackajeForSane Or targetFileName.Contains("/Split1.bak")) Then
                If ex.Message.ToString.Contains("file not found") Then
                    SaveTextExeption("در حال تلاش برای ساختن فولدر های مورد نیاز")
                    Dim index As Int16 = -1
                    Dim counter As Int16 = 0
                    Dim NewPath As String = ""
                    Dim NewLabDirectoryPath As String = ""
                    Dim NewLabSubDirectoryPath As String = ""
                    Dim LastPath As String = ""
                    Dim c As Char = ""
                    For Each c In sFtpFile
                        If c = "/" Then
                            index = counter
                        End If
                        counter += 1
                    Next


                    If index <> -1 Then
                        NewPath = sFtpFile.Substring(0, index)
                        LastPath = NewPath
                        counter = 0
                        For Each c In NewPath
                            If c = "/" Then
                                index = counter
                            End If
                            counter += 1
                        Next
                        NewLabSubDirectoryPath = sFtpFile.Substring(0, index)
                    End If

                    If index <> -1 Then
                        NewPath = NewLabSubDirectoryPath.Substring(0, index)
                        counter = 0
                        For Each c In NewPath
                            If c = "/" Then
                                index = counter
                            End If
                            counter += 1
                        Next
                        NewLabDirectoryPath = sFtpFile.Substring(0, index)
                    End If

                    FtpFolderCreate(NewLabDirectoryPath, ftpUser, ftpPassword, False)
                    FtpFolderCreate(NewLabSubDirectoryPath, ftpUser, ftpPassword, True)
                    FtpFolderCreate(LastPath, ftpUser, ftpPassword, True)
                    SaveTextExeption("پایان تلاش برای ساخت پوشه های مورد نیاز")
                    IsFirstPackajeForSane = False
                ElseIf ex.Message.ToString.Contains("The underlying connection was closed: An unexpected error occurred on a receive.") Then
                    If targetFileName.Contains("Split1.") Then
                        ErrorInfo = "ErrorSize"
                        DeleteFTPFile(sFtpFile)
                    End If
                ElseIf ex.Message.ToString.Contains("The operation has timed out") Then
                    If targetFileName.Contains("Split1.") Then
                        ErrorInfo = "ErrorSize"
                        DeleteFTPFile(sFtpFile)
                    End If
                End If
            ElseIf ex.Message.ToString.Contains("The underlying connection was closed: An unexpected error occurred on a receive.") Then
                If targetFileName.Contains("Split1.") Then
                    ErrorInfo = "ErrorSize"
                End If
                DeleteFTPFile(sFtpFile)
            ElseIf ex.Message.ToString.Contains("The operation has timed out") Then
                If targetFileName.Contains("Split1.") Then
                    ErrorInfo = "ErrorSize"
                    DeleteFTPFile(sFtpFile)
                End If
                'ElseIf ex.Message.Contains("Unable to connect to the remote server") Then
                '    If targetFileName.Contains("Split1") Then
                '        ErrorInfo = "ErrorSize"
                '    End If
                '    DeleteFTPFile(sFtpFile)

            Else
                SaveTextExeption("در ارسال اطلاعات به اف تی پی سرور خطایی رخ داده است. اررور : " + vbCrLf + ex.Message.ToString())
                'SaveTextExeption("Error 50 : In Send To Ftp : " + ex.Message.ToString())
                SaveTextExeption("Address In Ftp : " + sFtpFile)
            End If

            ExceptionInfo = ex
            Return False
        Finally

        End Try

    End Function

    Private Function FtpFolderCreate(folder_name As String, username As String, password As String, ISdayPath As Boolean) As Boolean
        Try
            Dim request As Net.FtpWebRequest = CType(FtpWebRequest.Create(folder_name), FtpWebRequest)
            request.Credentials = New NetworkCredential(username, password)
            request.Method = WebRequestMethods.Ftp.MakeDirectory
            Try
                Using response As FtpWebResponse = DirectCast(request.GetResponse(), FtpWebResponse)
                    ' Folder created
                End Using
            Catch ex As WebException
                Try
                    Dim response As FtpWebResponse = DirectCast(ex.Response, FtpWebResponse)
                    ' an error occurred
                    If response.StatusCode = FtpStatusCode.ActionNotTakenFileUnavailable Then
                    End If
                Catch ex1 As Exception

                    'SaveTextExeption("در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است ، در حال تلاش برای ساخت" + vbCrLf + folder_name + vbCrLf + ex1.Message.ToString())

                End Try

            End Try
            Return True
        Catch ex As Exception
            If ISdayPath Then
                SaveTextExeption("Error 49 : " + vbCrLf + "در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است" + vbCrLf + ex.Message.ToString())
            End If
            Return False
        End Try
    End Function


    Private Function FtpFolderDelete(folder_name As String, username As String, password As String, ISdayPath As Boolean) As Boolean
        Try
            Dim request As Net.FtpWebRequest = CType(FtpWebRequest.Create(folder_name), FtpWebRequest)
            request.Credentials = New NetworkCredential(username, password)
            request.Method = WebRequestMethods.Ftp.RemoveDirectory
            Try
                Using response As FtpWebResponse = DirectCast(request.GetResponse(), FtpWebResponse)
                    ' Folder created
                End Using
                MessageBox.Show("Folder 1 Ok", "Folder")
            Catch ex As WebException
                Try
                    Dim response As FtpWebResponse = DirectCast(ex.Response, FtpWebResponse)
                    ' an error occurred
                    If response.StatusCode = FtpStatusCode.ActionNotTakenFileUnavailable Then
                    End If
                    MessageBox.Show("Folder 2 Ok", "Folder")
                Catch ex1 As Exception

                    SaveTextExeption("در حذف پوشه بر روی اف تی پی سرور خطایی رخ داده است" + vbCrLf + folder_name + vbCrLf + ex1.Message.ToString())

                End Try

            End Try
            Return True
        Catch ex As Exception
            If ISdayPath Then
                SaveTextExeption("Error 49 : " + vbCrLf + "در حذف پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است" + vbCrLf + ex.Message.ToString())
            End If
            Return False
        End Try
    End Function

    Protected Sub DeleteFTPFile(ByVal FileName As String)
        Try
            'give simple file name a fully qualified locatin (based on my ftpURI contstant)
            Dim ftpfilename As String = FileName

            'create a FTPWebRequest object
            Dim ftpReq As FtpWebRequest = WebRequest.Create(ftpfilename)

            'set the method to delete the file
            ftpReq.Method = WebRequestMethods.Ftp.DeleteFile

            'delete the file
            Dim ftpResp As FtpWebResponse = ftpReq.GetResponse

        Catch ex As Exception
            SaveTextExeption("Error 51 : In Delete Ftp File : " + ex.Message.ToString())
            'spot to do error handling
        End Try
    End Sub

    Private Sub Btn_Logs_Click(sender As Object, e As EventArgs) Handles Btn_Logs.Click

        Try
            Dim ConnectionString As String = Txt_Connection.Text
            Dim dt As New DataTable("Dt")
            dt = GetLogs(ConnectionString)
            Dg_LastBackups.DataSource = dt
        Catch ex As Exception
            MessageBox.Show("Error 1 : " + ex.Message.ToString())
        End Try
    End Sub
    Public Function GetLogs(ByVal connection As String) As DataTable
        Dim dt As New DataTable("VerInfo")
        Dim con As SqlConnection = New SqlConnection(connection)
        Try
            con.Open()
            Dim Adp As New SqlClient.SqlDataAdapter("select Str_Type as 'نوع' , Str_Message as 'پیام' ,   Str_Date as 'تاریخ', Str_Time as 'ساعت', Str_FilePath as 'آدرس', Bit_Status as 'ضعیت', Bit_SendToFTP as 'ارسال به شرکت', Bit_Delete as 'حذف شده' from Tbl_PwsAutoBackupLogs where Str_Date = dbo.GetNowDate() order by Prk_PwsAutoBackupLog_AutoID desc", con)
            Adp.Fill(dt)
            con.Close()
        Catch ex As Exception
            SaveTextExeption("اررور در ذخیره لاگ" + ex.Message.ToString())
            con.Close()
            MessageBox.Show("Error 2 : " + ex.Message.ToString())
        End Try
        Return dt
    End Function

    Private Sub Dg_LastBackups_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs) Handles Dg_LastBackups.DataBindingComplete
        'change the color
        Try
            For i As Int16 = 0 To Dg_LastBackups.Rows.Count - 1
                If i Mod 2 = 0 Then
                    Dg_LastBackups.Rows(i).DefaultCellStyle.BackColor = Color.LightCyan
                Else
                    Dg_LastBackups.Rows(i).DefaultCellStyle.BackColor = Color.LightGray
                End If
            Next
            'change the color\
        Catch
        End Try
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
        Dim Path As String = ""
        'Path = "C:\BACKUP_For_Parsic_FTP\parsipol_developing09082021145540Diff\parsipol_developing09082021145540Diff.bak" ' dt.Rows(i)("Str_FilePath")

        FtpFolderDelete(TextBox1.Text, _FTPUsername, _FTPPassword, True)

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DeleteFileInFTP(TextBox2.Text)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Try
            FtpServer.Endpoint.Binding.CloseTimeout = New TimeSpan(0, 15, 0)
            FtpServer.Endpoint.Binding.ReceiveTimeout = New TimeSpan(0, 15, 0)
            FtpServer.Endpoint.Binding.OpenTimeout = New TimeSpan(0, 15, 0)
            FtpServer.Endpoint.Binding.SendTimeout = New TimeSpan(0, 15, 0)
            Dim AssemblerErrorLog As String = ""
            Dim ans As Boolean = FtpServer.Assembling(Txt_RootPath.Text, Txt_FtpLoclPath.Text, Txt_BackupName.Text, Txt_SplitName.Text, AssemblerErrorLog)
            If ans = True Then
                Txt_Answer.Text = "Completed"
            Else
                Txt_Answer.Text = AssemblerErrorLog
            End If

        Catch ex As Exception
            Txt_Answer.Text = Txt_Answer.Text + ex.Message.ToString()
        End Try

    End Sub
End Class


Public Class Parameter
    Public Info As Byte()
    Public FTPServerPath As String
    'Public FTPServerUsername As String
    'Public FTPServerPassword As String
    'Public FTPBackupSubPath As String
    'Public BackupFileName As String
    'Public ErrorInfo As String
    Sub New()
    End Sub
    Sub New(ByVal _Info As Byte(), ByVal _FTPServerPath As String) ', ByVal _FTPServerUsername As String, ByVal _FTPServerPassword As String, ByVal _FTPBackupSubPath As String, ByVal _BackupFileName As String, ByRef _ErrorInfo As String)
        Info = _Info
        FTPServerPath = _FTPServerPath
        'FTPServerUsername = _FTPServerUsername
        'FTPServerPassword = _FTPServerPassword
        'FTPBackupSubPath = _FTPBackupSubPath
        'BackupFileName = _BackupFileName
        'ErrorInfo = _ErrorInfo
    End Sub
End Class