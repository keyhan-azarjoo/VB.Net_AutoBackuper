Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Threading
Imports System.Data.SqlClient

Public Class Cls_DifferentialBackup

    '╔═══════════════════ Information ══════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Information"
    'Author: Keyhan Azarjo
    'Date : June 21,2020
#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝


    '╔════════════════════ Variable ════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Variable"

    Dim StopRecivingProcess As Boolean = False

    Public Thr_AutoDo As New Threading.Thread(AddressOf Th_AutoDo)

    Dim DTParsicMaster As DataTable = New DataTable("DTParsicMaster")
    Dim PWS As SupportTicket.Service1 = New SupportTicket.Service1
    Dim Assembler As FTPAssembler.WebService1
    Dim DoingFull As Boolean = False
    Dim IsFirstPackajeForSane As Boolean = True
    Public MyPublic As New Parsic.Public.CLS_Public
    Dim MySecurity As New Parsic.Business.Security.Cls_Encryption
    Dim DbTrueName As String = ""
    Dim ConnectionString As String = ""
    Dim DbBackupName As String = ""
    Dim DBBackupPath As String = ""
    Dim ErrorInfoLog As String = ""
    Dim Lock As Boolean = False
    Dim ErrorInBackup As Boolean = True
    Dim ErrorInDiff As Boolean = True
    Dim LabID As Int32 = 0
    Dim FullDiffLabsBackupLogsID As Int32 = 0
    Dim CloudFullDiffLabsBackupLogsID As Int32 = 0
    Dim FullBackupZipPath As String = ""
    Dim UpdatorTools As New UpdaterClasses.GetAndInsertVersionInDB(0)
    Dim GetSchedule As New DataTable()
    Dim ErrorLog As String = ""
    Dim con As SqlConnection
    Public threadFull As Thread
    Public threadDiff As Thread
    Public threadTransfer As Thread
    Public threadClear As Thread
    Public threadSmsSender As Thread
    Dim Firsttime As Boolean = True
    Dim SendToFTPOrder As Int32
    Dim tmpDb As Parsic.DataAccess.DBase

    Enum MessageMode
        Warning = 0
    End Enum

#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═══════════════════════ Event ════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Event"

    Public Event Message(ByVal Mode As MessageMode, ByVal Message As String)
    Public Event DeadLoopThread(Name As String, Dead As Boolean)

#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔════════════════════ Constructor ═════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Constructor"
    Sub New(ByVal _Mycommon As Parsic.Common.Cls_Common)

        Try
            SaveTextExeption("ساختن سرویس بک آپ گیری")

            MyCommon = _Mycommon

            tmpDb = New Parsic.DataAccess.DBase(MyCommon.MyDb.MyDbase.m_constr)

        Catch ex As Exception
            SaveTextExeption(ex.Message)
        End Try

    End Sub

#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔════════════════════ Function ════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Function"
    Public Function StartBackup() As Boolean


        Try
            RaiseEvent Message(MessageMode.Warning, "شروع سرویس / تابع بک آپ گیری" & "  ")
            SaveTextExeption("شروع سرویس / تابع بک آپ گیری")
            SaveBackupLogs(0, "شروع سرویس بک آپ گیر")

            If IsNothing(Thr_AutoDo) Then
                Thr_AutoDo = New Threading.Thread(AddressOf Th_AutoDo)
            End If

            StopRecivingProcess = False
            Thr_AutoDo.Start()



            Return True
        Catch ex As Exception
            RaiseEvent Message(MessageMode.Warning, "خطا در شروع بک آپ گیری" & "  " & ex.Message & vbCrLf & ex.StackTrace)
            RaiseEvent DeadLoopThread(Me.ToString, True)
            Return False
        End Try

    End Function

    Public Function StopBackup() As Boolean

        Try

            RaiseEvent Message(MessageMode.Warning, "توقف سرویس / تابع بک آپ گیری" & "  ")

            StopRecivingProcess = True

            If Not IsNothing(Thr_AutoDo) Then
                Thr_AutoDo.Abort()
                Thr_AutoDo = Nothing
            End If
            If Not IsNothing(threadDiff) Then
                threadDiff.Abort()
                threadDiff = Nothing
            End If
            If Not IsNothing(threadFull) Then
                threadFull.Abort()
                threadFull = Nothing
            End If
            If Not IsNothing(threadTransfer) Then
                threadTransfer.Abort()
                threadTransfer = Nothing
            End If

            If Not IsNothing(threadClear) Then
                threadClear.Abort()
                threadClear = Nothing
            End If

            Return True
        Catch ex As Exception
            RaiseEvent Message(MessageMode.Warning, "خطا در اتمام بک آپ گیری" & "  " & ex.Message & vbCrLf & ex.StackTrace)
            Return False
        End Try

    End Function

    Private Sub Th_AutoDo()
        Prerequirment()
        While StopRecivingProcess = False

            Try

                For Each dr As DataRow In GetSchedule.Rows
                    If dr("Bit_Enabled") Then
                        If dr("Str_Type") = "Full" Then
                            If IsNothing(threadFull) Then

                                SaveBackupLogs(0, " شروع گرفتن بک آپ فول برای تست اول")
                                Get_Full(dr, MyCommon.MyDb.MyDbase)
                                SaveBackupLogs(0, "شروع اولیه ی ترد بک آپ گیری فول")
                                threadFull = New Threading.Thread(DirectCast(Sub() DoFullThread(dr, MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                                threadFull.Start()
                            ElseIf threadFull.ThreadState = Threading.ThreadState.Stopped Or threadFull.ThreadState = Threading.ThreadState.StopRequested Or threadFull.ThreadState = Threading.ThreadState.Suspended Or threadFull.ThreadState = Threading.ThreadState.SuspendRequested Then
                                SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد بک آپ گیر فول استاپ و دوباره ران شد")
                                threadFull.Abort()
                                threadFull = Nothing
                                Threading.Thread.Sleep(10 * 1000)
                                threadFull = New Threading.Thread(DirectCast(Sub() DoFullThread(dr, MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                                threadFull.Start()
                            End If
                            Exit For
                        End If
                    End If
                Next
                Threading.Thread.Sleep(1000)
                For Each dr2 As DataRow In GetSchedule.Rows
                    If dr2("Bit_Enabled") Then
                        If dr2("Str_Type") = "Differential" Then
                            If IsNothing(threadDiff) Then
                                SaveBackupLogs(0, "شروع اولیه ی ترد بک آپ گیری دیف")
                                threadDiff = New Threading.Thread(DirectCast(Sub() DoDiffThread(dr2, MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                                threadDiff.Start()
                            ElseIf threadDiff.ThreadState = Threading.ThreadState.Stopped Or threadDiff.ThreadState = Threading.ThreadState.StopRequested Or threadDiff.ThreadState = Threading.ThreadState.Suspended Or threadDiff.ThreadState = Threading.ThreadState.SuspendRequested Then
                                SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد بک آپ گیر دیف استاپ و دوباره ران شد")
                                threadDiff.Abort()
                                threadDiff = Nothing
                                Threading.Thread.Sleep(30 * 1000)
                                threadDiff = New Threading.Thread(DirectCast(Sub() DoDiffThread(dr2, MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                                threadDiff.Start()
                            End If
                            Exit For
                        End If
                    End If
                Next



                'For i As Int16 = 0 To GetSchedule.Rows.Count() - 1
                '    If GetSchedule.Rows(i)("Bit_Enabled") Then
                '        If GetSchedule.Rows(i)("Str_Type") = "Full" Then

                '            If IsNothing(threadFull) Then
                '                SaveBackupLogs(0, "شروع اولیه ی ترد بک آپ گیری فول")
                '                threadFull = New Threading.Thread(DirectCast(Sub() DoFullThread(i, GetSchedule.Rows(i)("Str_Type"), MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                '                threadFull.Start()
                '            ElseIf threadFull.ThreadState = Threading.ThreadState.Stopped Or threadFull.ThreadState = Threading.ThreadState.StopRequested Or threadFull.ThreadState = Threading.ThreadState.Suspended Or threadFull.ThreadState = Threading.ThreadState.SuspendRequested Then
                '                SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد بک آپ گیر فول استاپ و دوباره ران شد")
                '                threadFull.Abort()
                '                threadFull = Nothing
                '                Threading.Thread.Sleep(30 * 1000)
                '                threadFull = New Threading.Thread(DirectCast(Sub() DoFullThread(i, GetSchedule.Rows(i)("Str_Type"), MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                '                threadFull.Start()
                '            End If
                '        End If


                '        Threading.Thread.Sleep(1000)
                '        If GetSchedule.Rows(i)("Str_Type") = "Differential" Then

                '            If IsNothing(threadDiff) Then
                '                SaveBackupLogs(0, "شروع اولیه ی ترد بک آپ گیری دیف")
                '                threadDiff = New Threading.Thread(DirectCast(Sub() DoDiffThread(i, GetSchedule.Rows(i)("Str_Type"), MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                '                threadDiff.Start()
                '            ElseIf threadDiff.ThreadState = Threading.ThreadState.Stopped Or threadDiff.ThreadState = Threading.ThreadState.StopRequested Or threadDiff.ThreadState = Threading.ThreadState.Suspended Or threadDiff.ThreadState = Threading.ThreadState.SuspendRequested Then
                '                SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد بک آپ گیر دیف استاپ و دوباره ران شد")
                '                threadDiff.Abort()
                '                threadDiff = Nothing
                '                Threading.Thread.Sleep(30 * 1000)
                '                threadDiff = New Threading.Thread(DirectCast(Sub() DoDiffThread(i, GetSchedule.Rows(i)("Str_Type"), MyCommon.MyDb.MyDbase), Threading.ThreadStart))
                '                threadDiff.Start()
                '            End If

                '        End If
                '    Else
                '        SaveBackupLogs(0, "تنظیمات بک آپ گیر در سنترال غیر فعال میباشد  ")
                '    End If
                'Next



                If IsNothing(threadTransfer) Then
                    SaveBackupLogs(0, "شروع اولیه ی ترد ارسال کننده ی بک آپ ها ")
                    threadTransfer = New System.Threading.Thread(AddressOf TransferToFTP)
                    threadTransfer.Start(MyCommon.MyDb.MyDbase)
                ElseIf threadTransfer.ThreadState = Threading.ThreadState.Stopped Or threadTransfer.ThreadState = Threading.ThreadState.StopRequested Or threadTransfer.ThreadState = Threading.ThreadState.Suspended Or threadTransfer.ThreadState = Threading.ThreadState.SuspendRequested Then
                    SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد ارسال کننده ی بک آپ ها استاپ و دوباره ران شد")
                    threadTransfer.Abort()
                    threadTransfer = Nothing
                    Threading.Thread.Sleep(30 * 1000)
                    threadTransfer = New System.Threading.Thread(AddressOf TransferToFTP)
                    threadTransfer.Start(MyCommon.MyDb.MyDbase)
                End If



                'SaveTextExeption("شروع سرویس / تابع FTP")
                'threadTransfer = New Threading.Thread(AddressOf TransferToFTP)
                'threadTransfer.Start(TMPDBNEW)


                If IsNothing(threadClear) Then
                    SaveBackupLogs(0, "شروع اولیه ی ترد پاک کننده ی بک آپ ها")
                    threadClear = New System.Threading.Thread(AddressOf CleareBackups)
                    threadClear.Start(MyCommon.MyDb.MyDbase)
                ElseIf threadClear.ThreadState = Threading.ThreadState.Stopped Or threadClear.ThreadState = Threading.ThreadState.StopRequested Or threadClear.ThreadState = Threading.ThreadState.Suspended Or threadClear.ThreadState = Threading.ThreadState.SuspendRequested Then
                    SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد پاک کننده ی بک آپ ها استاپ و دوباره ران شد")
                    threadClear.Abort()
                    threadClear = Nothing
                    Threading.Thread.Sleep(30 * 1000)
                    threadClear = New System.Threading.Thread(AddressOf CleareBackups)
                    threadClear.Start(MyCommon.MyDb.MyDbase)
                End If



                ' به مهندس حبیبی داده شد ایشون اجرا میکنند
                'SaveTextExeption("شروع سرویس / تابع ارسال کننده اس ام اس بک آپ ها")  
                'threadSmsSender = New System.Threading.Thread(AddressOf SendErrorSMS)
                'threadSmsSender.Start(TmpDbNew)
                Thread.Sleep(15 * 60 * 1000)

            Catch ex As Exception
                'RaiseEvent Message(MessageMode.Warning, "خطا در ترد بک آپ گیری مادر(اصلی) رخ داده است" & "  " & ex.Message & vbCrLf & ex.StackTrace)
                SaveBackupLogs(0, "بخاطر وجود یک مشکل ترد مادر(اصلی) با خطا مواجه شد و دوباره اجرا میشود")
                'RaiseEvent DeadLoopThread(Me.ToString, True)
                'StopRecivingProcess = True
            End Try
        End While
    End Sub


    Public Function Prerequirment()
        'do prerequiremnts
        Try
            Dim ans As String = ChekTimeForIISandSQL(tmpDb)
            If (ans = "OK") Then
            Else
                SaveTextExeption("خطا در چک کردن تایم دو سیستم سرور اسکیوال و آ آی اس " + ans)
                SaveBackupLogs(0, "خطا در چک کردن تایم دو سیستم سرور اسکیوال و آ آی اس " + ans)
            End If
            GetDBInfo()
            Try
                tmpDb.dt_filler(GetSchedule, "execute SP_Get_BackupSchedule", CommandType.Text)
            Catch ex As Exception
                SaveBackupLogs(0, "اررور در گرفتن اطلاعات تنظیمات بک آپ " + "  Error : " + ex.Message.ToString())
            End Try
            CheckDefaultInfo(DbTrueName + ",", tmpDb)
            Try
                If GetSchedule.Rows.Count() = 0 Then
                    SaveTextExeption("Schedule Not Found")
                    ErrorInfoLog = "هیچ تنظیماتی برای بک آپ گیری اتوماتیک ذخیره نشده است"
                    SaveBackupLogs(2, "تنظیمات بک آپ گیر در سنترال یافت نشد، لطفا تنظیمات بک آپ گیری را در سنترال چک نمایید")
                End If
            Catch ex As Exception
                SaveBackupLogs(0, "تنظیمات بک آپ گیر در سنترال یافت نشد، لطفا تنظیمات بک آپ گیری را در سنترال چک نمایید" + ex.Message.ToString())
            End Try
            SaveBackupLogs(0, "تنظیمات بک آپ گیر در سنترال واکشی شد" + "")
            'do prerequiremnts\
        Catch ex As Exception
            SaveBackupLogs(0, "خطا در پری ریکوایرمنت بک آپ ها(چک کردن تایم سرور ها و واکشی تنظیمات بک آپ گیری)")
        End Try
    End Function

    'Public Function SendErrorSMS(_Tmpdb As Parsic.DataAccess.DBase)

    '    SaveTextExeption("شروع تابع بک آپ گیر و چک کننده بک آپ ها برای اس ام اس")
    '    While True
    '        'SaveTextExeption("sleep for : " + SleepTime.ToString() + " Min")
    '        'Thread.Sleep(120 * 60 * 1000)
    '        'Dim SleepTime As Integer = FindTimeDifference(10, 0)
    '        'Thread.Sleep((SleepTime) * 60 * 1000)
    '        Try
    '            CheckBackupIsTaken(_Tmpdb)
    '        Catch ex As Exception
    '            SaveTextExeption("Error 1012 : " + ex.Message.ToString())
    '        End Try

    '    End While


    'End Function
    'Public Function CheckBackupIsTaken(_Tmpdb As Parsic.DataAccess.DBase)
    '    Try
    '        Dim dt As New DataTable("DT")
    '        Dim DtPhone As New DataTable("DT")
    '        Dim DtParsicMemberPhone As New DataTable("DT")
    '        Dim DtNameID As New DataTable("DT")
    '        tmpDb.dt_filler(dt, "select * from Tbl_Full_Diff_BackupLogs where Int_Status = 1 and cast(Str_BackupSize as int)>1 and Str_StartDate = DATEADD(DAY, -1, dbo.GetNowDate())", CommandType.Text)
    '        SaveTextExeption("تعداد لاگ های دیروز که بک آپ فول یا دیف درست گرفته اند : " + dt.Rows.Count().ToString())
    '        If dt.Rows.Count() >= 1 Then
    '        Else
    '            'send sms
    '            Dim SMS_Queue As New Cls_SMS_Queue(MyCommon)
    '            Dim NameIDComm As String = "select * from TBL_Option where Option_ID like 'RecieptLabName' or Option_ID like 'ParsicLabID'"
    '            tmpDb.dt_filler(DtNameID, NameIDComm, CommandType.Text)
    '            Dim LabName As String = ""
    '            Dim LabID As String = ""
    '            For ii As Int16 = 0 To DtNameID.Rows.Count() - 1
    '                If (DtNameID.Rows(ii)("Option_ID") = "RecieptLabName") Then
    '                    LabName = DtNameID.Rows(ii)("Option_Value").ToString()
    '                ElseIf (DtNameID.Rows(ii)("Option_ID") = "ParsicLabID") Then
    '                    LabID = DtNameID.Rows(ii)("Option_Value").ToString()
    '                End If
    '            Next

    '            Dim message As String = "آزمایشگاه " + LabName + " کد " + LabID + vbCrLf + "سرویس بکاپ اتوماتیک آزمایشگاه در 24 ساعت گذشته موفق به گرفتن بکاپ نشده است. لطفا در اسرع وقت با پشتیبانی پارسیپل 02149951122 تماس بفرمایید"
    '            SaveTextExeption(message)

    '            Dim comm As String = "select Str_Mobile from Tbl_SecEmployees where Str_Mobile <> '' and (Bit_FinancialManager=1 or Bit_NotifyForSimpleStorage =1)"
    '            tmpDb.dt_filler(DtPhone, comm, CommandType.Text)






    '            For k As Int16 = 0 To DtPhone.Rows.Count() - 1
    '                'SaveTextExeption(DtPhone.Rows(k)("Str_Mobile"))
    '                SMS_Queue.Queue_SystemNotification(DtPhone.Rows(k)("Str_Mobile"), message, "سیستمی")
    '            Next



    '            DtParsicMemberPhone = New DataTable("DT")

    '            Dim Membercomm As String = "select option_value from TBL_Option where Option_ID like 'AutoBackupLogNumber'"
    '            tmpDb.dt_filler(DtParsicMemberPhone, Membercomm, CommandType.Text)

    '            Dim numbers As String = DtParsicMemberPhone.Rows(0)("option_value").ToString()

    '            Dim number As String = ""
    '            For jj As Int16 = 0 To numbers.Count() - 1

    '                If (numbers(jj) = ",") Then
    '                    'SaveTextExeption(Number(jj))
    '                    SMS_Queue.Queue_SystemNotification(number, message, "سیستمی")
    '                    number = ""
    '                Else
    '                    number += numbers(jj)
    '                End If


    '            Next

    '            ''SMS_Queue.Queue_SystemNotification("09016900161", message, "سیستمی")
    '            ''SMS_Queue.Queue_SystemNotification("09123105310", message, "سیستمی")
    '            ''SMS_Queue.Queue_SystemNotification("09385344410", message, "سیستمی")




    '            SaveTextExeption("Error SMSs Sent")
    '        End If
    '    Catch ex As Exception
    '        SaveTextExeption("Error 1015 : " + ex.Message.ToString())
    '    End Try

    'End Function



    Public Function CheckAndGetBackup() As String
        Try


        Catch ex As Exception
            SaveTextExeption("Error 01 : " + ex.Message.ToString())
        End Try
        Return "Nothing"
    End Function

    Public Sub GetDBInfo()
        Try
            tmpDb.dt_filler(DTParsicMaster, "Select * From Db_ParsicMaster.dbo.TBL_DBList", CommandType.Text)
            LabID = MyCommon.MySajaOption.GetSysOption("ParsicLabID")
        Catch ex As Exception
            SaveBackupLogs(0, "اررور در گرفتن اطلاعات پارسیک مستر " + "  Error : " + ex.Message.ToString())
        End Try
        Try
            DbTrueName = MyCommon.MyDb.DBList_Name
            tmpDb.EXECmd("Update Tbl_BackupSchedule Set Str_DbNames=Replace(Str_DbNames,'Nothing','') + '" & DbTrueName & "' + ',' Where Str_DbNames not like '%" & DbTrueName & "%'")
        Catch ex As Exception
            SaveBackupLogs(0, "اررور در ویرایش جدول برنامه ریزی بکاپ اگر اولین اجرا بعد از ست کردن بک آپ میباشد، جدول :  " + DbTrueName + "  Error : " + ex.Message.ToString())
        End Try
    End Sub




    'Public Function DoThread(ByVal num As Int16, ByVal ThreadType As String, _Tmpdb As Parsic.DataAccess.DBase)
    '    Try
    '        SaveBackupLogs(num, " شروع ترد بک آپ گیری فول")
    '        Dim OccuresOnAt As Boolean = GetSchedule.Rows(num)("Bit_OccuresOnAt")
    '        Dim OccuresOnAtTime As String = GetSchedule.Rows(num)("Str_OccuresOnAtTime")
    '        Dim OccuresEvery As Boolean = GetSchedule.Rows(num)("Bit_OccuresEvery")
    '        Dim OccuresEveryMinute As Integer = GetSchedule.Rows(num)("Str_OccuresEveryMinute")
    '        Dim StartAtTime As String = GetSchedule.Rows(num)("Str_StartAtTime")
    '        Dim FinishAtTime As String = GetSchedule.Rows(num)("Str_FinishAtTime")

    '        Dim Type As String = GetSchedule.Rows(num)("Str_Type")
    '        Dim OcuureType As String = GetSchedule.Rows(num)("Str_Ocuure")
    '        Dim DayList As New List(Of String)
    '        If OcuureType = "Weekly" Then

    '            If GetSchedule.Rows(num)("Bit_Saturday") Then
    '                DayList.Add("Saturday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Sunday") Then
    '                DayList.Add("Sunday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Monday") Then
    '                DayList.Add("Monday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Tuesday") Then
    '                DayList.Add("Tuesday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Wednesday") Then
    '                DayList.Add("Wednesday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Thursday") Then
    '                DayList.Add("Thursday")
    '            End If
    '            If GetSchedule.Rows(num)("Bit_Friday") Then
    '                DayList.Add("Friday")
    '            End If

    '        End If


    '        While StopRecivingProcess = False
    '            Try
    '                ErrorInBackup = True
    '                ErrorInDiff = True
    '                If OcuureType = "Weekly" Then
    '                    If DayList.Contains(GetDay()) Then


    '                        If OccuresOnAt Then
    '                            Dim hour As Int32 = GetHour(OccuresOnAtTime)
    '                            Dim Min As Int32 = GetMin(OccuresOnAtTime)
    '                            If hour = Now.Hour And Min = Now.Minute Then
    '                                'DOOOOOOO
    '                                While True
    '                                    If Lock = False Then
    '                                        Lock = True
    '                                        SaveBackupLogs(num, " شروع گرفتن بک آپ")
    '                                        Get_Full_Diff(num, Type, _Tmpdb)
    '                                        Lock = False
    '                                        Exit While
    '                                    Else
    '                                        Thread.Sleep(1000)
    '                                    End If
    '                                End While


    '                                Thread.Sleep(60 * 1000)

    '                                Dim s As String = "0"
    '                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)
    '                                SaveTextExeption("bbb1Wo4 Sleep for min ->" + SleepTime.ToString())
    '                                Thread.Sleep((SleepTime - 2) * 60 * 1000)

    '                            Else
    '                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)
    '                                SaveTextExeption("bbb1Wo5 Sleep for min ->" + SleepTime.ToString())
    '                                If SleepTime < 3 Then
    '                                    Thread.Sleep(30 * 1000)
    '                                Else
    '                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)
    '                                End If
    '                            End If
    '                        Else
    '                            Dim StartHour As Int32 = GetHour(StartAtTime)
    '                            Dim StartMin As Int32 = GetMin(StartAtTime)
    '                            Dim FinishHour As Int32 = GetHour(FinishAtTime)
    '                            Dim FinishMin As Int32 = GetMin(FinishAtTime)

    '                            Dim StartTime As String = ""
    '                            Dim FinishTime As String = ""
    '                            StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
    '                            FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"
    '                            SaveTextExeption("bbb2We2 Start at " + StartTime + " Finish at " + FinishTime)
    '                            Dim CurTime As String = ""
    '                            CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"


    '                            If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
    '                                'DOOOOOOO
    '                                SaveTextExeption("ساعت در بازه زمانی میباشد")
    '                                While True
    '                                    If Lock = False Then
    '                                        Lock = True
    '                                        SaveBackupLogs(4, " شروع گرفتن بک آپ")
    '                                        Get_Full_Diff(num, Type, _Tmpdb)
    '                                        Lock = False
    '                                        Exit While
    '                                    Else
    '                                        Thread.Sleep(1000)
    '                                    End If
    '                                End While

    '                                Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
    '                                SaveTextExeption("bbb2We6 Occure every min --->" + OccuresEveryMinute.ToString())
    '                            Else
    '                                Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
    '                                SaveTextExeption("bbb2We7  Sleep for min ->" + SleepTime.ToString())
    '                                If SleepTime < 3 Then
    '                                    Thread.Sleep(30 * 1000)
    '                                Else
    '                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)

    '                                End If
    '                            End If


    '                        End If

    '                    Else

    '                        Dim SleepTime As Integer = FindTimeDifference(0, 0)
    '                        SaveTextExeption("bbb3 Sleep for min --->" + SleepTime.ToString())
    '                        If SleepTime < 3 Then
    '                            Thread.Sleep(30 * 1000)
    '                        Else
    '                            Thread.Sleep((SleepTime - 2) * 60 * 1000)
    '                        End If

    '                    End If
    '                ElseIf OcuureType = "Daily" Then
    '                    SaveTextExeption("bbb4")
    '                    If OccuresOnAt Then
    '                        Dim hour As Int32 = GetHour(OccuresOnAtTime)
    '                        Dim Min As Int32 = GetMin(OccuresOnAtTime)
    '                        If hour = Now.Hour And Min = Now.Minute Then
    '                            'DOOOOOOO
    '                            While True
    '                                If Lock = False Then
    '                                    Lock = True
    '                                    SaveBackupLogs(4, " شروع گرفتن بک آپ")
    '                                    Get_Full_Diff(num, Type, _Tmpdb)
    '                                    Lock = False
    '                                    Exit While
    '                                Else
    '                                    Thread.Sleep(1000)
    '                                End If
    '                            End While

    '                            Thread.Sleep(60 * 1000)
    '                            Dim SleepTime As Integer = FindTimeDifference(hour, Min)
    '                            SaveTextExeption("bbb5" + SleepTime.ToString())
    '                            Thread.Sleep((SleepTime - 2) * 60 * 1000)

    '                        Else
    '                            Dim SleepTime As Integer = FindTimeDifference(hour, Min)
    '                            SaveTextExeption("bbb6 Sleep for min --->" + SleepTime.ToString())
    '                            If SleepTime < 3 Then
    '                                Thread.Sleep(30 * 1000)
    '                            Else
    '                                Thread.Sleep((SleepTime - 2) * 60 * 1000)
    '                            End If
    '                        End If
    '                    Else

    '                        Dim StartHour As Int32 = GetHour(StartAtTime)
    '                        Dim StartMin As Int32 = GetMin(StartAtTime)
    '                        Dim FinishHour As Int32 = GetHour(FinishAtTime)
    '                        Dim FinishMin As Int32 = GetMin(FinishAtTime)

    '                        Dim StartTime As String = ""
    '                        Dim FinishTime As String = ""
    '                        StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
    '                        FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"

    '                        Dim CurTime As String = ""
    '                        CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"

    '                        If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
    '                            'DOOOOOOO
    '                            While True
    '                                If Lock = False Then
    '                                    Lock = True
    '                                    SaveBackupLogs(4, " شروع گرفتن بک آپ")
    '                                    Get_Full_Diff(num, Type, _Tmpdb)
    '                                    Lock = False
    '                                    Exit While
    '                                Else
    '                                    Thread.Sleep(1000)
    '                                End If
    '                            End While

    '                            Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
    '                            SaveTextExeption("bbb8 Occure at ---> " + OccuresEveryMinute.ToString())
    '                        Else
    '                            Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
    '                            SaveTextExeption("bbb9 Sleep for min ---> " + SleepTime.ToString())
    '                            If SleepTime < 3 Then
    '                                Thread.Sleep(30 * 1000)
    '                            Else
    '                                Thread.Sleep((SleepTime - 2) * 60 * 1000)

    '                            End If
    '                        End If


    '                    End If

    '                End If


    '            Catch ex As Exception
    '                SaveTextExeption("Error 02- : " + ex.Message.ToString())
    '                Thread.Sleep(30 * 1000)
    '            End Try



    '        End While
    '    Catch ex As Exception
    '        SaveTextExeption("Error 03 : " + ex.Message.ToString())
    '        'Errorr
    '    End Try
    '    Return True
    'End Function











    Public Function DoFullThread(ByRef DR As DataRow, _Tmpdb As Parsic.DataAccess.DBase)
        Try
            SaveBackupLogs(0, " شروع ترد بک آپ گیری فول")
            Dim OccuresOnAt As Boolean = DR("Bit_OccuresOnAt")
            Dim OccuresOnAtTime As String = DR("Str_OccuresOnAtTime")
            Dim OccuresEvery As Boolean = DR("Bit_OccuresEvery")
            Dim OccuresEveryMinute As Integer = DR("Str_OccuresEveryMinute")
            Dim StartAtTime As String = DR("Str_StartAtTime")
            Dim FinishAtTime As String = DR("Str_FinishAtTime")

            Dim Type As String = DR("Str_Type")
            Dim OcuureType As String = DR("Str_Ocuure")
            Dim DayList As New List(Of String)
            If OcuureType = "Weekly" Then

                If DR("Bit_Saturday") Then
                    DayList.Add("Saturday")
                End If
                If DR("Bit_Sunday") Then
                    DayList.Add("Sunday")
                End If
                If DR("Bit_Monday") Then
                    DayList.Add("Monday")
                End If
                If DR("Bit_Tuesday") Then
                    DayList.Add("Tuesday")
                End If
                If DR("Bit_Wednesday") Then
                    DayList.Add("Wednesday")
                End If
                If DR("Bit_Thursday") Then
                    DayList.Add("Thursday")
                End If
                If DR("Bit_Friday") Then
                    DayList.Add("Friday")
                End If

            End If



            While StopRecivingProcess = False
                Try
                    ErrorInBackup = True
                    ErrorInDiff = True
                    If OcuureType = "Weekly" Then
                        If DayList.Contains(GetDay()) Then


                            If OccuresOnAt Then
                                Dim hour As Int32 = GetHour(OccuresOnAtTime)
                                Dim Min As Int32 = GetMin(OccuresOnAtTime)
                                If hour = Now.Hour And Min = Now.Minute Then
                                    'DOOOOOOO
                                    If Firsttime Then
                                        Dim SleepTime1 As Integer = FindTimeDifference(hour, Min)
                                        SaveTextExeption("Full Weekly occurre at 1 : Sleep for min ---> " + SleepTime1.ToString())
                                        Thread.Sleep((SleepTime1 - 10) * 60 * 1000)
                                    End If
                                    Firsttime = False
                                    While True
                                        If Lock = False Then
                                                Lock = True
                                                SaveBackupLogs(0, " شروع گرفتن بک آپ فول")
                                                Dim myans As String = Get_Full(DR, _Tmpdb)
                                                If myans.Contains("Finish FullBackup is Taking") Then
                                                    Lock = True
                                                Else
                                                    Lock = False
                                                End If
                                                Exit While
                                            Else
                                                Thread.Sleep(5000)
                                            End If
                                        End While

                                        Thread.Sleep(60 * 1000)

                                    Dim s As String = "0"
                                    Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                    SaveTextExeption("Full Weekly occurre at 1 : Sleep for min ---> " + SleepTime.ToString())
                                    Thread.Sleep((SleepTime - 10) * 60 * 1000)

                                Else
                                    Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                    SaveTextExeption("Full weekly occurre at 2 : Sleep for min ---> " + SleepTime.ToString())
                                    If SleepTime < 12 Then
                                        Thread.Sleep(30 * 1000)
                                    Else
                                        Thread.Sleep((SleepTime - 5) * 60 * 1000)
                                    End If
                                End If
                            Else
                                Dim StartHour As Int32 = GetHour(StartAtTime)
                                Dim StartMin As Int32 = GetMin(StartAtTime)
                                Dim FinishHour As Int32 = GetHour(FinishAtTime)
                                Dim FinishMin As Int32 = GetMin(FinishAtTime)

                                Dim StartTime As String = ""
                                Dim FinishTime As String = ""
                                StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
                                FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"
                                SaveTextExeption("Full Start at " + StartTime + " Finish at " + FinishTime)
                                Dim CurTime As String = ""
                                CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"


                                If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
                                    'DOOOOOOO

                                    SaveTextExeption("ساعت در بازه زمانی میباشد و بک آپ فول گرفته میشود")
                                        While True
                                            If Lock = False Then
                                                Lock = True
                                                SaveBackupLogs(4, " شروع گرفتن بک آپ فول")
                                                Dim myans As String = Get_Full(DR, _Tmpdb)
                                                If myans.Contains("Finish FullBackup is Taking") Then
                                                    Lock = True
                                                Else
                                                    Lock = False
                                                End If
                                                Exit While
                                            Else
                                                Thread.Sleep(1000)
                                            End If
                                        End While

                                    Firsttime = False
                                    Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
                                    SaveTextExeption("Full Weekly every Some Min 1 : Sleep for min ---> " + OccuresEveryMinute.ToString())
                                Else
                                    Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
                                    SaveTextExeption("Full Weekly every Some Min 2 : Sleep for min ---> " + SleepTime.ToString())
                                    If SleepTime < 3 Then
                                        Thread.Sleep(30 * 1000)
                                    Else
                                        Thread.Sleep((SleepTime - 2) * 60 * 1000)

                                    End If
                                End If


                            End If

                        Else

                            Dim SleepTime As Integer = FindTimeDifference(0, 0)
                            SaveTextExeption("Full Weekly Sleep for a day, min ---> " + SleepTime.ToString() + " - 15")
                            If SleepTime < 15 Then
                                Thread.Sleep(30 * 1000)
                            Else
                                Thread.Sleep((SleepTime - 2) * 60 * 1000)
                            End If

                        End If
                    ElseIf OcuureType = "Daily" Then
                        If OccuresOnAt Then
                            Dim hour As Int32 = GetHour(OccuresOnAtTime)
                            Dim Min As Int32 = GetMin(OccuresOnAtTime)
                            If hour = Now.Hour And Min = Now.Minute Then
                                'DOOOOOOO
                                If Firsttime Then
                                    Dim SleepTime1 As Integer = FindTimeDifference(hour, Min)
                                    SaveTextExeption("Full Daily occurred at 1 : Sleep for min ---> " + SleepTime1.ToString() + " - 10")
                                    Thread.Sleep((SleepTime1 - 10) * 60 * 1000)
                                End If
                                Firsttime = False

                                While True
                                    If Lock = False Then
                                        Lock = True
                                        SaveBackupLogs(4, " شروع گرفتن بک آپ فول")
                                        Dim myans As String = Get_Full(DR, _Tmpdb)
                                        If myans.Contains("Finish FullBackup is Taking") Then
                                            Lock = True
                                        Else
                                            Lock = False
                                        End If
                                        Exit While
                                    Else
                                        Thread.Sleep(1000)
                                    End If
                                End While

                                Thread.Sleep(60 * 1000)
                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                SaveTextExeption("Full Daily occurred at 1 : Sleep for min ---> " + SleepTime.ToString() + " - 10")
                                Thread.Sleep((SleepTime - 10) * 60 * 1000)

                            Else
                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                SaveTextExeption("Full Daily occurred at 2 : Sleep for min ---> " + SleepTime.ToString())
                                If SleepTime < 12 Then
                                    Thread.Sleep(30 * 1000)
                                Else
                                    Thread.Sleep((SleepTime - 5) * 60 * 1000)
                                End If
                            End If
                        Else

                            Dim StartHour As Int32 = GetHour(StartAtTime)
                            Dim StartMin As Int32 = GetMin(StartAtTime)
                            Dim FinishHour As Int32 = GetHour(FinishAtTime)
                            Dim FinishMin As Int32 = GetMin(FinishAtTime)

                            Dim StartTime As String = ""
                            Dim FinishTime As String = ""
                            StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
                            FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"

                            Dim CurTime As String = ""
                            CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"

                            If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
                                'DOOOOOOO
                                While True
                                        If Lock = False Then
                                            Lock = True
                                            SaveBackupLogs(4, " شروع گرفتن بک آپ فول")
                                            Dim myans As String = Get_Full(DR, _Tmpdb)
                                            If myans.Contains("Finish FullBackup is Taking") Then
                                                Lock = True
                                            Else
                                                Lock = False
                                            End If
                                            Exit While
                                        Else
                                            Thread.Sleep(1000)
                                        End If
                                    End While

                                SaveTextExeption("Full Daily every Some Min 1 : Sleep for min ---> " + OccuresEveryMinute.ToString())
                                Firsttime = False
                                Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
                            Else
                                Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
                                SaveTextExeption("Full Daily every Some Min 2 : Sleep for min ---> " + SleepTime.ToString())
                                If SleepTime < 3 Then
                                    Thread.Sleep(30 * 1000)
                                Else
                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)

                                End If
                            End If


                        End If

                    End If


                Catch ex As Exception
                    SaveTextExeption("Error 02- : " + ex.Message.ToString())
                    Thread.Sleep(30 * 1000)
                End Try

            End While
        Catch ex As Exception
            SaveTextExeption("Error 03 : " + ex.Message.ToString())
            'Errorr
        End Try
        Return True
    End Function


    Public Function DoDiffThread(ByVal Dr As DataRow, _Tmpdb As Parsic.DataAccess.DBase)
        Try
            SaveBackupLogs(0, " شروع ترد بک آپ گیری دیف")
            Dim OccuresOnAt As Boolean = Dr("Bit_OccuresOnAt")
            Dim OccuresOnAtTime As String = Dr("Str_OccuresOnAtTime")
            Dim OccuresEvery As Boolean = Dr("Bit_OccuresEvery")
            Dim OccuresEveryMinute As Integer = Dr("Str_OccuresEveryMinute")
            Dim StartAtTime As String = Dr("Str_StartAtTime")
            Dim FinishAtTime As String = Dr("Str_FinishAtTime")

            Dim Type As String = Dr("Str_Type")
            Dim OcuureType As String = Dr("Str_Ocuure")
            Dim DayList As New List(Of String)
            If OcuureType = "Weekly" Then

                If Dr("Bit_Saturday") Then
                    DayList.Add("Saturday")
                End If
                If Dr("Bit_Sunday") Then
                    DayList.Add("Sunday")
                End If
                If Dr("Bit_Monday") Then
                    DayList.Add("Monday")
                End If
                If Dr("Bit_Tuesday") Then
                    DayList.Add("Tuesday")
                End If
                If Dr("Bit_Wednesday") Then
                    DayList.Add("Wednesday")
                End If
                If Dr("Bit_Thursday") Then
                    DayList.Add("Thursday")
                End If
                If Dr("Bit_Friday") Then
                    DayList.Add("Friday")
                End If

            End If


            While StopRecivingProcess = False
                Try
                    SaveTextExeption("Diff Start While")
                    ErrorInBackup = True
                    ErrorInDiff = True
                    If OcuureType = "Weekly" Then
                        If DayList.Contains(GetDay()) Then


                            If OccuresOnAt Then
                                Dim hour As Int32 = GetHour(OccuresOnAtTime)
                                Dim Min As Int32 = GetMin(OccuresOnAtTime)
                                If hour = Now.Hour And Min = Now.Minute Then
                                    SaveTextExeption("Diff Weekly 1 , occurre at , ساعت در بازه زمانی میباشد و بک آپ دیف گرفته میشود")
                                    'DOOOOOOO
                                    While True
                                        If Lock = False Then
                                            Lock = True
                                            SaveBackupLogs(0, "شروع گرفتن بک آپ دیف")
                                            SaveTextExeption("Diff Weekly 1 , occurre at , time ok, Diff Start")
                                            Dim myans As String = Get_Diff(Dr, _Tmpdb)
                                            If myans.Contains("Finish FullBackup is Taking") Then
                                                Lock = True
                                            Else
                                                Lock = False
                                            End If
                                            Exit While
                                        Else
                                            Thread.Sleep(1000)
                                        End If
                                    End While


                                    Thread.Sleep(60 * 1000)

                                    Dim s As String = "0"
                                    Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                    SaveTextExeption("Diff Weekly 1 , occurre at , backup has just been taken : Sleep for " + (SleepTime - 2).ToString() + " Min  or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds")
                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                    SaveTextExeption("Diff Weekly 1 , occurre at , it has just woken up for backup")
                                Else
                                    SaveTextExeption("Diff Weekly 0 , occurre at , CurrentTime =: " + Now.Hour + ":" + Now.Minute + " and Backup Time : " + hour + ":" + Min)
                                    Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                    If SleepTime < 3 Then
                                        SaveTextExeption("Diff Weekly 2 , occurre at , Time is not in the given time, Sleep for " + (30 * 1000).ToString() + " milliseconds")
                                        Thread.Sleep(30 * 1000)
                                        SaveTextExeption("Diff Weekly 2 , occurre at , it has just woken up for backup")
                                    Else
                                        SaveTextExeption("Diff Weekly 3 , occurre at , Time is not in the given time, Sleep for " + (SleepTime - 2).ToString() + " Min  or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds")
                                        Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                        SaveTextExeption("Diff Weekly 3 , occurre at , it has just woken up for backup")

                                    End If
                                End If
                            Else
                                Dim StartHour As Int32 = GetHour(StartAtTime)
                                Dim StartMin As Int32 = GetMin(StartAtTime)
                                Dim FinishHour As Int32 = GetHour(FinishAtTime)
                                Dim FinishMin As Int32 = GetMin(FinishAtTime)

                                Dim StartTime As String = ""
                                Dim FinishTime As String = ""
                                StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
                                FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"
                                SaveTextExeption("Diff Start at " + StartTime + " Finish at " + FinishTime)
                                Dim CurTime As String = ""
                                CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"


                                If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
                                    'DOOOOOOO
                                    SaveTextExeption("Diff Weekly 1 , every some min , ساعت در بازه زمانی میباشد و بک آپ دیف گرفته میشود")

                                    While True
                                        If Lock = False Then
                                            Lock = True
                                            SaveBackupLogs(4, "شروع گرفتن بک آپ دیف")
                                            SaveTextExeption("Diff Weekly 1 , every some min , time ok, Diff Start")
                                            Dim myans As String = Get_Diff(Dr, _Tmpdb)
                                            If myans.Contains("Finish FullBackup is Taking") Then
                                                Lock = True
                                            Else
                                                Lock = False
                                            End If
                                            Exit While
                                        Else
                                            Thread.Sleep(1000)
                                        End If
                                    End While

                                    SaveTextExeption("Diff Weekly 1 , every some min , Sleep for " + OccuresEveryMinute.ToString() + " min")
                                    Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
                                    SaveTextExeption("Diff Weekly 1 , every some min , it has just woken up for backup")

                                Else
                                    SaveTextExeption("Diff Weekly 0 , every some min , Time is not in the given range, CurrentTime : " + CurTime + " and StartTime : " + StartTime + " and FinishTime : " + FinishTime)
                                    Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
                                    If SleepTime < 3 Then
                                        SaveTextExeption("Diff Weekly 2 , every some min , Time is not in the given range, Sleep for " + (30 * 1000).ToString() + "milliseconds")
                                        Thread.Sleep(30 * 1000)
                                        SaveTextExeption("Diff Weekly 2 , every some min , it has just woken up for backup")
                                    Else
                                        SaveTextExeption("Diff Weekly 3 , every some min , Time is not in the given range, Sleep for " + (SleepTime - 2).ToString() + " min or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds")
                                        Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                        SaveTextExeption("Diff Weekly 3 , every some min , it has just woken up for backup")
                                    End If
                                End If


                            End If

                        Else

                            Dim SleepTime As Integer = FindTimeDifference(0, 0)
                            SaveTextExeption("Diff Weekly Sleep for a day, " + SleepTime.ToString() + " min")
                            If SleepTime < 3 Then
                                Thread.Sleep(30 * 1000)
                            Else
                                Thread.Sleep((SleepTime - 2) * 60 * 1000)
                            End If

                        End If
                    ElseIf OcuureType = "Daily" Then
                        If OccuresOnAt Then
                            Dim hour As Int32 = GetHour(OccuresOnAtTime)
                            Dim Min As Int32 = GetMin(OccuresOnAtTime)
                            If hour = Now.Hour And Min = Now.Minute Then
                                SaveTextExeption("Diff Daily 1 , occurre at, ساعت در بازه زمانی میباشد و بک آپ دیف گرفته میشود")
                                'DOOOOOOO
                                While True
                                    If Lock = False Then
                                        Lock = True
                                        SaveBackupLogs(4, "شروع گرفتن بک آپ دیف")
                                        SaveTextExeption("Diff Daily 1 , occurre at, time ok, Diff Start")
                                        Dim myans As String = Get_Diff(Dr, _Tmpdb)
                                        If myans.Contains("Finish FullBackup is Taking") Then
                                            Lock = True
                                        Else
                                            Lock = False
                                        End If
                                        Exit While
                                    Else
                                        Thread.Sleep(1000)
                                    End If
                                End While

                                Thread.Sleep(60 * 1000)
                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)
                                SaveTextExeption("Diff Daily 1 , occurre at, backup has just been taken : Sleep for " + (SleepTime - 2).ToString() + " Min  or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds")
                                Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                SaveTextExeption("Diff Daily 1 , occurre at, it has just woken up for backup")
                            Else
                                SaveTextExeption("Diff Daily 0 , occurre at, CurrentTime : " + Now.Hour + ":" + Now.Minute + " and Backup Time : " + hour + ":" + Min)
                                Dim SleepTime As Integer = FindTimeDifference(hour, Min)

                                If SleepTime < 3 Then
                                    SaveTextExeption("Diff Daily 2 , occurre at , Time is not in the given time, Sleep for " + (30 * 1000).ToString() + " milliseconds")
                                    Thread.Sleep(30 * 1000)
                                    SaveTextExeption("Diff Daily 2 , occurre at , it has just woken up for backup")
                                Else
                                    SaveTextExeption("Diff Daily 3 , occurre at , Time is not in the given time, Sleep for " + (SleepTime - 2).ToString() + " Min  or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds")
                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                    SaveTextExeption("Diff Daily 3 , occurre at , it has just woken up for backup")
                                End If



                            End If
                        Else

                            Dim StartHour As Int32 = GetHour(StartAtTime)
                            Dim StartMin As Int32 = GetMin(StartAtTime)
                            Dim FinishHour As Int32 = GetHour(FinishAtTime)
                            Dim FinishMin As Int32 = GetMin(FinishAtTime)

                            Dim StartTime As String = ""
                            Dim FinishTime As String = ""
                            StartTime = IIf(StartHour.ToString.Length = 1, "0", "") & StartHour & ":" & IIf(StartMin.ToString.Length = 1, "0", "") & StartMin & ":00"
                            FinishTime = IIf(FinishHour.ToString.Length = 1, "0", "") & FinishHour & ":" & IIf(FinishMin.ToString.Length = 1, "0", "") & FinishMin & ":00"

                            Dim CurTime As String = ""
                            CurTime = IIf(Now.Hour.ToString.Length = 1, "0", "") & Now.Hour & ":" & IIf(Now.Minute.ToString.Length = 1, "0", "") & Now.Minute & ":00"

                            If Convert.ToDateTime(StartTime) <= Convert.ToDateTime(CurTime) And Convert.ToDateTime(FinishTime) >= Convert.ToDateTime(CurTime) Then
                                SaveTextExeption("Diff Daily 1 , every some min , Lock = " + Lock.ToString() + " , ساعت در بازه زمانی میباشد و بک آپ دیف گرفته میشود DDES")
                                'DOOOOOOO
                                While True
                                    If Lock = False Then
                                        Lock = True
                                        SaveBackupLogs(4, "شروع گرفتن بک آپ دیف")
                                        SaveTextExeption("Diff Daily 1 , every some min , time ok, Diff Start DDES")
                                        Dim myans As String = Get_Diff(Dr, _Tmpdb)
                                        If myans.Contains("Finish FullBackup is Taking") Then
                                            Lock = True
                                        Else
                                            Lock = False
                                        End If
                                        Exit While
                                    Else
                                        SaveTextExeption("Diff Daily 1 , every some min , Sleep 1 Sec DDES")
                                        Thread.Sleep(1000)
                                    End If
                                End While

                                SaveTextExeption("Diff Daily 1 , every some min , Sleep for " + OccuresEveryMinute.ToString() + " min DDES")
                                Thread.Sleep((OccuresEveryMinute) * 60 * 1000)
                                SaveTextExeption("Diff Daily 1 , every some min , it has just woken up for backupDDES")
                            Else
                                SaveTextExeption("Diff Daily 0 , every some min , Time is not in the given range , CurrentTime : " + CurTime + " and StartTime : " + StartTime + " and FinishTime : " + FinishTime + " DDES")
                                Dim SleepTime As Integer = FindTimeDifference(StartHour, StartMin)
                                If SleepTime < 3 Then
                                    SaveTextExeption("Diff Daily 2 , every some min , Time is not in the given range, Sleep for " + (30 * 1000).ToString() + "milliseconds DDES")
                                    Thread.Sleep(30 * 1000)
                                    SaveTextExeption("Diff Daily 2 , every some min , it has just woken up for backup DDES")
                                Else
                                    SaveTextExeption("Diff Daily 3 , every some min , Time is not in the given range, Sleep for " + (SleepTime - 2).ToString() + " min or " + ((SleepTime - 2) * 60 * 1000).ToString() + "milliseconds DDES")
                                    Thread.Sleep((SleepTime - 2) * 60 * 1000)
                                    SaveTextExeption("Diff Daily 3 , every some min , it has just woken up for backup DDES")
                                End If

                            End If


                        End If

                    End If


                Catch ex As Exception
                    SaveTextExeption("Error 02- : " + ex.Message.ToString())
                    Thread.Sleep(30 * 1000)
                End Try

            End While
        Catch ex As Exception
            SaveTextExeption("Error 03 : " + ex.Message.ToString())
            'Errorr
        End Try
        Return True
    End Function
















    'Public Function Get_Full_Diff(ByVal num As Int32, ByVal Type As String, _Tmpdb As Parsic.DataAccess.DBase)
    '    Dim ans1 As String = ""
    '    Dim ans2 As String = ""
    '    Try

    '        Dim IISNetworkPath As String = GetSchedule.Rows(num)("Str_IISNetworkPath")
    '        Dim IISPath As String = GetSchedule.Rows(num)("Str_IISPath")
    '        Dim BackupSchedule_ID As String = GetSchedule.Rows(num)("Prk_BackupSchedule_AutoID")
    '        Try
    '            If IISNetworkPath(IISNetworkPath.Length - 1) = "\" Then
    '                IISNetworkPath = IISNetworkPath.Substring(0, IISNetworkPath.Length - 1)
    '            End If
    '            If IISPath(IISPath.Length - 1) = "\" Then
    '                IISPath = IISPath.Substring(0, IISPath.Length - 1)
    '            End If
    '            Try
    '                If (Directory.Exists(IISNetworkPath)) Then
    '                    SaveBackupLogs(num, " مسیر تایید شد، مسیر : " + IISNetworkPath)
    '                Else
    '                    SaveBackupLogs(num, " مسیر وارد شده در دسترس نمیباشد، مسیر : " + IISNetworkPath)
    '                End If

    '            Catch ex As Exception

    '            End Try

    '        Catch ex As Exception
    '            SaveTextExeption("Error 04 : In Paths")
    '            SaveBackupLogs(num, " اررور در شناسایی مسیر : " + IISNetworkPath + " اررور : " + ex.Message.ToString())
    '            'Error In Paths
    '        End Try
    '        Dim DBNames As String = GetSchedule.Rows(num)("Str_DbNames")
    '        'SaveBackupLogs(num,  GetSchedule.Rows(num)("Str_Type").tostring())
    '        If GetSchedule.Rows(num)("Str_Type") = "Full" Then

    '            SaveBackupLogs(num, " شروع تابع اصلی برای بک آپ ")
    '            ans1 = FullBackup(BackupSchedule_ID, IISNetworkPath, DBNames, ErrorLog, False, _Tmpdb, False)

    '            Return " Full Backup Finish" + vbCrLf + ans1
    '        End If


    '        If GetSchedule.Rows(num)("Str_Type") = "Differential" Then


    '            Dim AllTrueDBNames As String = GetSchedule.Rows(num)("Str_DbNames")
    '            SaveBackupLogs(num, " شروع تابع اصلی برای دیفرنشیال")
    '            ans2 = GetDiffBackup(BackupSchedule_ID, IISNetworkPath, AllTrueDBNames, ErrorLog, _Tmpdb)

    '            Return "Diff Backup Finish" + vbCrLf + ans2

    '        End If
    '    Catch ex As Exception
    '        SaveTextExeption("Error 05 : " + ex.Message.ToString() + vbCrLf + "Full : " + ans1 + vbCrLf + "Diff : " + ans2)
    '    End Try

    'End Function


    Public Function Get_Full(ByVal Dr As DataRow, _Tmpdb As Parsic.DataAccess.DBase)
        If DoingFull = True Then
            SaveBackupLogs(0, "بک آپ فول در حال گرفتن است، به همین دلیل بک آپ فول ديگري گرفته نميشود")
            Return "Finish FullBackup is Taking, Full Canceled"
        End If
        Dim ans1 As String = ""
        Dim ans2 As String = ""
        Try

            Dim IISNetworkPath As String = Dr("Str_IISNetworkPath")
            Dim IISPath As String = Dr("Str_IISPath")
            Dim BackupSchedule_ID As String = Dr("Prk_BackupSchedule_AutoID")
            Try
                If IISNetworkPath(IISNetworkPath.Length - 1) = "\" Then
                    IISNetworkPath = IISNetworkPath.Substring(0, IISNetworkPath.Length - 1)
                End If
                If IISPath(IISPath.Length - 1) = "\" Then
                    IISPath = IISPath.Substring(0, IISPath.Length - 1)
                End If
                Try
                    If (Directory.Exists(IISNetworkPath)) Then
                        SaveBackupLogs(0, " مسیر فول تایید شد، مسیر : " + IISNetworkPath)
                    Else
                        SaveBackupLogs(0, " مسیر فول وارد شده در دسترس نمیباشد، مسیر : " + IISNetworkPath)
                    End If

                Catch ex As Exception

                End Try

            Catch ex As Exception
                SaveTextExeption("Error 04 : In Paths")
                SaveBackupLogs(0, " اررور در شناسایی مسیر فول : " + IISNetworkPath + " اررور : " + ex.Message.ToString())
                'Error In Paths
            End Try
            Dim DBNames As String = Dr("Str_DbNames")
            'SaveBackupLogs(0,  dr("Str_Type").tostring())

            SaveBackupLogs(0, " شروع تابع اصلی برای بک آپ فول ")
            ans1 = GetFullBackup(BackupSchedule_ID, IISNetworkPath, DBNames, ErrorLog, False, _Tmpdb, False)
            Return " Full Backup Finish" + vbCrLf + ans1

        Catch ex As Exception
            SaveTextExeption("Error 05 : " + ex.Message.ToString() + vbCrLf + "Full : " + ans1 + vbCrLf + "Diff : " + ans2)
        End Try

    End Function

    Public Function Get_Diff(ByVal Dr As DataRow, _Tmpdb As Parsic.DataAccess.DBase)
        If DoingFull = True Then
            SaveBackupLogs(0, "بک آپ فول در حال گرفتن است، به همین دلیل بک آپ دیف کنسل شد")
            Return "Finish FullBackup is Taking, Diff Canceled"
        End If
        Dim ans1 As String = ""
        Dim ans2 As String = ""

        Try

            Dim IISNetworkPath As String = Dr("Str_IISNetworkPath")
            Dim IISPath As String = Dr("Str_IISPath")
            Dim BackupSchedule_ID As String = Dr("Prk_BackupSchedule_AutoID")
            Try
                If IISNetworkPath(IISNetworkPath.Length - 1) = "\" Then
                    IISNetworkPath = IISNetworkPath.Substring(0, IISNetworkPath.Length - 1)
                End If
                If IISPath(IISPath.Length - 1) = "\" Then
                    IISPath = IISPath.Substring(0, IISPath.Length - 1)
                End If
                Try
                    If (Directory.Exists(IISNetworkPath)) Then
                        SaveBackupLogs(0, "مسیر دیف تایید شد، مسیر : " + IISNetworkPath)
                    Else
                        SaveBackupLogs(0, " مسیر دیف وارد شده در دسترس نمیباشد، مسیر : " + IISNetworkPath)
                        Return "Error Path"
                    End If

                Catch ex As Exception

                End Try

            Catch ex As Exception
                SaveTextExeption("Error 04 : In Paths")
                SaveBackupLogs(0, " اررور در شناسایی مسیر دیف : " + IISNetworkPath + " اررور : " + ex.Message.ToString())
                'Error In Paths
            End Try
            Dim DBNames As String = Dr("Str_DbNames")
            'SaveBackupLogs(0,  dr("Str_Type").tostring())



            Dim AllTrueDBNames As String = Dr("Str_DbNames")
            SaveBackupLogs(0, " شروع تابع اصلی برای دیفرنشیال")
            ans2 = GetDiffBackup(BackupSchedule_ID, IISNetworkPath, AllTrueDBNames, ErrorLog, _Tmpdb)
            Return "Diff Backup Finish" + vbCrLf + ans2

        Catch ex As Exception
            SaveTextExeption("Error 05 : " + ex.Message.ToString() + vbCrLf + "Full : " + ans1 + vbCrLf + "Diff : " + ans2)
        End Try

    End Function


    Public Function GetFullBackup(BackupScheduleID As Int32, DBBackupPath As String, AllTrueDBNames As String, Str_ErrorLog As String, FromDif As Boolean, _Tmpdb As Parsic.DataAccess.DBase, ByVal _FromDiff As Boolean) As String
        Dim ans As String = ""
        Try

            Dim LDBN() As String = AllTrueDBNames.Substring(0, AllTrueDBNames.Length - 1).Split(",")
            For i As Int16 = 0 To LDBN.Count() - 1
                DbTrueName = LDBN(i)


                If ChekLastBackupForThreeDaysIsOk(DbTrueName, _Tmpdb) = False And _FromDiff = False Then
                    SaveTextExeption("تاریخ آخرین بک آپ تا به امروز کمتر از 1 روز بوده است و بک آپ گرفته نمی شود!" & DbTrueName)
                    SaveBackupLogs(6, "تاریخ آخرین بک آپ تا به امروز کمتر از 1 روز بوده است و بک آپ گرفته نمی شود!")
                Else

                    Dim mybackupPath As String = ""
                    If Directory.Exists(DBBackupPath + "\" + DbTrueName) Then
                    Else
                        Directory.CreateDirectory(DBBackupPath + "\" + DbTrueName)
                        SaveBackupLogs(7, " ساخت پوشه ی بک آپ فول" + DBBackupPath + "\" + DbTrueName)
                    End If

                    Dim DBName As String
                    'UpdatorTools = New GetAndInsertVersionInDB(0)
                    DBName = DbTrueName + "_" + Date.Now.Year.ToString() + "_" + Date.Now.Month.ToString() + "_" + Date.Now.Day.ToString() + "_" + Date.Now.Hour.ToString() + "_" + Date.Now.Minute.ToString()
                    mybackupPath = DBBackupPath + "\" + DbTrueName + "\" + DBName + "bak"

                    Directory.CreateDirectory(mybackupPath)

                    Dim BackupPath As String = mybackupPath + "\" + DBName + ".bak"
                    Dim LastPationID As Integer = _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("select Top 1 PRK_AdmitPatient from TBL_AdmitPatient with(nolock) order by PRK_AdmitPatient desc"))
                    Try
                        FullDiffLabsBackupLogsID = _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("execute SP_Insert_Full_Diff_BackupLogs @Frk_BackupScheduleID = " + BackupScheduleID.ToString() + " , @Frk_Repository = -1, @Str_DBName = '" + DbTrueName + "', @Int_LastReception = " + LastPationID.ToString() + ", @Str_BackupType = 'Full', @Str_DBBackupPath = N'" + mybackupPath + ".zip" + "', @Str_ErrorLog = N'" + Str_ErrorLog + "', @Str_Description = N'', @Int_Status = 0 "))
                    Catch ex As Exception
                        SaveTextExeption("ارر در ارسال لاگ داخلی بک آپ" + vbCrLf + "execute SP_Insert_Full_Diff_BackupLogs @Frk_BackupScheduleID = " + BackupScheduleID.ToString() + " , @Frk_Repository = -1, @Str_DBName = '" + DbTrueName + "', @Int_LastReception = " + LastPationID.ToString() + ", @Str_BackupType = 'Full', @Str_DBBackupPath = N'" + mybackupPath + ".zip" + "', @Str_ErrorLog = N'" + Str_ErrorLog + "', @Str_Description = N'', @Int_Status = 0 ")
                    End Try

                    'Try
                    '    CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)
                    'Catch ex As Exception
                    '    SaveBackupLogs(11, "اررور در ارسال لاگ بک آپ فول به سرور ابری")

                    'End Try
                    If (ErrorLog <> "") Then
                        ans = "Error : " + Str_ErrorLog
                    End If

                    DoingFull = True
                    Dim taskans As String = BackupAdvance(DbTrueName, BackupPath, _Tmpdb)
                    DoingFull = False
                    If taskans = "Complete" Then
                        CreateZipFile(mybackupPath, mybackupPath + ".zip")


                        File.Delete(BackupPath)
                        Directory.Delete(mybackupPath)
                        ans = mybackupPath + ".zip"
                    Else
                        ans = "Error In BackupAdvance Function" + vbCrLf + taskans
                    End If
                    Dim SecondAns As String = ""
                    If ans.Contains("Error") = False Then
                        Try
                            Dim LastBackupStartTime As String = LastBackupInLocalDB("Full", DbTrueName, _Tmpdb).Rows(0)("backup_start_date").ToString()
                            Dim fileLength As Long
                            Dim chk As String = ""
                            Try
                                Dim SR As New IO.StreamReader(ans)
                                fileLength = SR.BaseStream.Length
                                Dim WholeByte As Byte()
                                WholeByte = UpdatorTools.StreamFile(ans)
                                chk = UpdatorTools.CalculateChecksum(WholeByte)
                            Catch ex As Exception
                                SaveTextExeption("Error 15.2 : " + ex.Message.ToString())
                            End Try
                            'Send Full Backup Complate Log to cloud And Send To Ftp

                            Try
                                _Tmpdb.EXECmd("execute SP_Update_Full_Diff_BackupLogs @FullDiffLabsBackupLogsID = " + FullDiffLabsBackupLogsID.ToString() + ", @Str_BackupSize = '" + fileLength.ToString() + "', @Str_ChkSum = '" + chk + "' , @Str_Backup_start_date_for_check =N'" + LastBackupStartTime + "', @Str_FinishDate = '', @Str_FinishTime = '', @Str_IISBackupPath = N'', @Str_ErrorLog = N'',  @Bit_TransferToFTP = False , @Str_FtpPath = N'',  @Str_Description = N'', @Int_Status = 1 ")
                            Catch ex As Exception
                                SaveTextExeption("ارسال لاگ به بانک داخلی خطا دارد ولی بک آپ بدون لاگ گرفته میشود" + ex.Message.ToString())
                                SaveBackupLogs(14, "ارسال لاگ به بانک داخلی خطا دارد ولی بک آپ فول بدون لاگ گرفته میشود" + ex.Message.ToString())
                            End Try

                            Try
                                CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)

                                Dim Status As Int16 = 0
                                Try
                                    If Convert.ToInt64(fileLength) > 1 Then
                                        Status = 1
                                    End If
                                Catch ex As Exception

                                End Try
                                PWS.AutoBackup_Edit_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, CloudFullDiffLabsBackupLogsID, fileLength.ToString(), chk, "", "", "", Status)
                            Catch ex As Exception
                                SaveTextExeption("ارسال لاگ به فضای ابری خطا دارد ولی بک آپ بدون لاگ گرفته میشود" + ex.Message.ToString())
                                SaveBackupLogs(14, "ارسال لاگ به فضای ابری خطا دارد ولی بک آپ فول بدون لاگ گرفته میشود" + ex.Message.ToString())
                            End Try

                            SecondAns = "Full_Diff Backup Ok"
                        Catch ex As Exception
                        End Try

                    Else
                        'Send Full Error Log to cloud
                        Try
                            If FromDif Then
                                _Tmpdb.EXECmd("execute SP_Update_Full_Diff_BackupLogs @FullDiffLabsBackupLogsID = " + FullDiffLabsBackupLogsID.ToString() + ", @Str_BackupSize = '', @Str_ChkSum = '' , @Str_Backup_start_date_for_check =N'', @Str_FinishDate = '', @Str_FinishTime = '', @Str_IISBackupPath = N'', @Str_ErrorLog = N' " + ans + "',  @Bit_TransferToFTP = False , @Str_FtpPath = N'',  @Str_Description = N' بک آپ غیر از بک آپ اتوماتیک گرفته شده است، در زمان تلاش برای بک آپ گیری فول ارور بوجود آمده است', @Int_Status = 1 ")
                            Else
                                _Tmpdb.EXECmd("execute SP_Update_Full_Diff_BackupLogs @FullDiffLabsBackupLogsID = " + FullDiffLabsBackupLogsID.ToString() + ", @Str_BackupSize = '', @Str_ChkSum = '' , @Str_Backup_start_date_for_check =N'', @Str_FinishDate = '', @Str_FinishTime = '', @Str_IISBackupPath = N'', @Str_ErrorLog = N' " + ans + "',  @Bit_TransferToFTP = False , @Str_FtpPath = N'',  @Str_Description = N'در زمان تلاش برای بک آپ گیری فول ارور بوجود آمده است', @Int_Status = 1 ")
                            End If
                        Catch ex As Exception
                            SaveTextExeption("ارسال لاگ فول به بانک داخلی خطا دارد" + ex.Message.ToString())
                            SaveBackupLogs(14, "ارسال لاگ فول به بانک داخلی خطا دارد " + ex.Message.ToString())
                        End Try
                        Try
                            If FromDif Then
                                CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)
                                PWS.AutoBackup_Edit_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, CloudFullDiffLabsBackupLogsID.ToString(), "", "", "", ans, " بک آپ غیر از بک آپ اتوماتیک گرفته شده است، در زمان تلاش برای بک آپ گیری فول ارور بوجود آمده است", 1)
                            Else
                                CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)
                                PWS.AutoBackup_Edit_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, CloudFullDiffLabsBackupLogsID.ToString(), "", "", "", ans, "در زمان تلاش برای بک آپ گیری فول ارور بوجود آمده است", 1)
                            End If
                        Catch ex As Exception
                            SaveTextExeption("ارسال لاگ فول به فضای ابری خطا دارد" + ex.Message.ToString())
                            SaveBackupLogs(14, "ارسال لاگ فول به فضای ابری خطا دارد " + ex.Message.ToString())

                        End Try

                        SecondAns = "Full_Diff Backup Error"

                    End If
                End If
            Next
        Catch ex As Exception
            Return "Error 12 : In FullBackup Function" + ex.Message.ToString()
            SaveBackupLogs(15, "اررور 89 :" + ex.Message.ToString())

        End Try
        Return "Error Backup" + ans
    End Function

    Public Function GetDiffBackup(BackupScheduleID As Int32, DBBackupPath As String, AllTrueDBNames As String, Str_ErrorLog As String, _Tmpdb As Parsic.DataAccess.DBase) As String
        Try
            Dim LDBN() As String = AllTrueDBNames.Substring(0, AllTrueDBNames.Length - 1).Split(",")
            For i As Int16 = 0 To LDBN.Count() - 1

                DbTrueName = LDBN(i)


                Dim Chek As Boolean = ChekLastBackupIsTrue(DbTrueName, _Tmpdb)
                If Chek = False Then
                    SaveBackupLogs(0, "آخرین بک آپ با بک آپ پارسی پل یکی نمیباشد و برای گرفتن دیفرنشیال ابتدا باید بک آپ فول گرفته شود")
                    Dim ans1 As String = GetFullBackup(BackupScheduleID, DBBackupPath, DbTrueName + ",", ErrorLog, True, _Tmpdb, True)
                    Return ans1
                End If




                Dim mybackupPath As String = ""
                If Directory.Exists(DBBackupPath + "\" + DbTrueName) Then
                Else
                    Directory.CreateDirectory(DBBackupPath + "\" + DbTrueName)
                End If
                mybackupPath = DBBackupPath + "\" + DbTrueName

                Dim ans As String = ""
                Dim DBName As String
                'UpdatorTools = New GetAndInsertVersionInDB(0)
                DBName = DbTrueName + "_" + Date.Now.Year.ToString() + "_" + Date.Now.Month.ToString() + "_" + Date.Now.Day.ToString() + "_" + Date.Now.Hour.ToString() + "_" + Date.Now.Minute.ToString()
                mybackupPath = DBBackupPath + "\" + DbTrueName + "\" + DBName + "dif"
                Directory.CreateDirectory(mybackupPath)
                Dim BackupPath As String = mybackupPath + "\" + DBName + ".dif"
                Dim LastPationID As Integer = -1
                Try
                    LastPationID = _Tmpdb.EXECScalar("select Top 1 PRK_AdmitPatient from TBL_AdmitPatient order by PRK_AdmitPatient desc")
                Catch ex As Exception
                    SaveTextExeption("Error 13 : " + ex.Message.ToString())
                End Try
                Try
                    FullDiffLabsBackupLogsID = _Tmpdb.EXECScalar("execute SP_Insert_Full_Diff_BackupLogs @Frk_BackupScheduleID = " + BackupScheduleID.ToString() + ", @Str_DBName = '" + DbTrueName + "', @Int_LastReception = " + LastPationID.ToString() + ", @Str_BackupType = 'Diff', @Str_DBBackupPath = N'" + mybackupPath + ".zip" + "', @Str_ErrorLog = N'" + Str_ErrorLog + "', @Str_Description = N'', @Int_Status = 0 ")
                    'CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Diff", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)

                Catch ex As Exception
                    SaveTextExeption("Error 14 : " + ex.Message.ToString())
                End Try
                If (ErrorLog <> "") Then
                    ans = "Error : " + Str_ErrorLog
                End If

                Dim ans2 As String = DiffAdvance(DbTrueName, BackupPath, _Tmpdb)

                If ans2 = "Complete" Then
                    Thread.Sleep(10)
                    CreateZipFile(mybackupPath, mybackupPath + ".zip")


                    File.Delete(BackupPath)
                    Directory.Delete(mybackupPath)
                    ans = mybackupPath + ".zip"



                Else
                    ans = "Error In BackupAdvance Function = Error : " + ans2
                End If


                Dim SecondAns As String = ""

                If ans.Contains("Error") <> True Then
                    Try
                        'در جدول لاگ دیفرنشیال آخرین تاریخ بک آپ فول را ذخیره می کند
                        Dim LastFullBackupStartTime As String = LastBackupInLocalDB("Full", DbTrueName, _Tmpdb).Rows(0)("backup_start_date").ToString()

                        Dim fileLength As Long
                        Dim chk As String = ""
                        Try
                            Dim SR As New IO.StreamReader(ans)
                            fileLength = SR.BaseStream.Length
                            Dim WholeByte As Byte()
                            WholeByte = UpdatorTools.StreamFile(ans)
                            chk = UpdatorTools.CalculateChecksum(WholeByte)
                        Catch ex As Exception
                            SaveTextExeption("Error 15 : " + ex.Message.ToString())
                        End Try


                        'Send Diff Backup Complate Log to cloud  And Send To Ftp
                        Try

                            _Tmpdb.EXECmd("execute SP_Update_Full_Diff_BackupLogs @FullDiffLabsBackupLogsID = " + FullDiffLabsBackupLogsID.ToString() + ", @Str_BackupSize = '" + fileLength.ToString() + "', @Str_ChkSum = '" + chk + "' , @Str_Backup_start_date_for_check =N'" + LastFullBackupStartTime + "', @Str_FinishDate = '', @Str_FinishTime = '', @Str_IISBackupPath = N'', @Str_ErrorLog = N'',  @Bit_TransferToFTP = False , @Str_FtpPath = N'',  @Str_Description = N'', @Int_Status = 1 ")
                            CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)
                            Dim Status As Int16 = 0
                            Try
                                If Convert.ToInt64(fileLength) > 1 Then
                                    Status = 1
                                End If
                            Catch ex As Exception

                            End Try
                            PWS.AutoBackup_Edit_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, CloudFullDiffLabsBackupLogsID, fileLength.ToString(), chk, "", "", "", Status)

                        Catch ex As Exception
                            SaveBackupLogs(0, "ارسال لاگ دیف به بانک خطا دارد ولی بک آپ بدون لاگ گرفته میشود" + ex.Message.ToString())
                            SaveTextExeption("ارسال لاگ به بانک خطا دارد ولی بک آپ بدون لاگ گرفته میشود، این مشکل باید گزارش شود" + ex.Message.ToString())
                        End Try

                        SecondAns = "Diff Backup Ok"
                    Catch ex As Exception
                        SaveTextExeption("Error 16 : " + ex.Message.ToString())
                        'SecondAns = "Error in DiffBackup"
                    End Try

                Else
                    'Send Diff Error Log to cloud
                    Try
                        _Tmpdb.EXECmd("execute SP_Update_Full_Diff_BackupLogs @FullDiffLabsBackupLogsID = " + FullDiffLabsBackupLogsID.ToString() + ", @Str_BackupSize = '', @Str_ChkSum = '' , @Str_Backup_start_date_for_check =N'', @Str_FinishDate = '', @Str_FinishTime = '', @Str_IISBackupPath = N'', @Str_ErrorLog = N'" + ans + "',  @Bit_TransferToFTP = False , @Str_FtpPath = N'',  @Str_Description = N'', @Int_Status = 1 ")
                        CloudFullDiffLabsBackupLogsID = PWS.AutoBackup_Set_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, FullDiffLabsBackupLogsID, BackupScheduleID, LabID, DbTrueName, "Full", "", "", LastPationID, "", mybackupPath + ".zip", Str_ErrorLog, "", 0)
                        PWS.AutoBackup_Edit_Local_Full_Diff_BackupLogsForAllLabs(Get_UserName, Get_Password, CloudFullDiffLabsBackupLogsID.ToString(), "", "", "", ans, "", 1)
                    Catch ex As Exception
                        SaveBackupLogs(0, "ارسال لاگ دیف به بانک خطا دارد ولی بک آپ بدون لاگ گرفته میشود، پیغام خطا : " + ex.Message.ToString())

                        SaveTextExeption("ارسال لاگ دیف به بانک خطا دارد ولی بک آپ بدون لاگ گرفته میشود، پیغام خطا : " + ex.Message.ToString())
                    End Try

                    SecondAns = "Diff Backup Error"
                End If

            Next
        Catch ex As Exception
            Return "Error 17 : In FullBackup Function"
        End Try
        Return "Error Backup"
    End Function

    Public Function BackupAdvance(ByVal DBList_Name As String, ByVal BackupPath As String, _Tmpdb As Parsic.DataAccess.DBase) As String

        Dim _MyDb As New Parsic.DataAccess.DBase(_Tmpdb.m_constr)

        Try
            'SaveTextExeption("شروع بکاپ گیری")
            SaveBackupLogs(0, "در حال تلاش برای گرفتن بک آپ فول")
            Dim DateInfo As String = ""
            Dim LogInfo As String = ""

            '====================================================================================
            Threading.Thread.Sleep(250)
            Dim Counter As Int16 = 0
            Dim ErrorMessage As String = ""
            While Counter <= 10
                Try
                    _MyDb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH INIT, CHECKSUM, NOFORMAT, COMPRESSION", DataAccess.DBase.ExecuteMode._NONQEURY, 1800)
                    SaveBackupLogs(9, "بک آپ فول با موفقیت ذخیره شد")
                    ErrorInBackup = False
                    Exit While
                Catch ex As Exception
                    If ex.Message.ToString.Contains("COMPRESSION") Then
                        SaveTextExeption("Error 24 : " + vbCrLf + "اررور در بک آپ گیری با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())
                        SaveBackupLogs(8, "اررور در بک آپ گیری فول با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی ")
                        _MyDb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH INIT, CHECKSUM, NOFORMAT", DataAccess.DBase.ExecuteMode._NONQEURY, 1800)
                        SaveBackupLogs(9, "بک آپ فول با موفقیت ذخیره شد")
                        ErrorInBackup = False
                        Exit While
                    ElseIf ex.Message.ToString.Contains("There is already an open DataReader associated with this Command which must be closed first") Then
                        ErrorMessage = ex.Message.ToString()
                        Threading.Thread.Sleep(1000 * 6)
                    ElseIf (ex.Message.ToString.Contains("access")) Then
                        SaveBackupLogs(10, "در دسترسی به آدرس شبکه برای بک آپ فول خطا بوجود آمده است، لطفا موارد پایین را چک نمایید:" + vbCrLf + "دسترسی فولدر شبکه را برای همه بگزارید" + vbCrLf + "سرویس اسکیوال و اسکیوال ایجنسی را در سرور اسکیوال با یوزر ادمین ران کنید" + vbCrLf + "اسکریپت دسترسی در فایل پی دی اف را اعمال کنید" + ex.Message.ToString())
                        Exit While
                    Else
                        SaveTextExeption("Error 25 :" + vbCrLf + "اررور در انجام تسک بک آپ گیری اسکیوال " + ex.Message.ToString())
                        SaveBackupLogs(10, "اررور در گرفتن بک آپ فول  " + ex.Message.ToString())
                        Return "Error" + ex.Message.ToString()
                        Exit While
                    End If

                End Try
                Counter += 1
            End While

            If Counter >= 10 Then
                SaveBackupLogs(0, "اررور در انجام تسک بک آپ گیری، ده بار تلاش برای بک آپ گیری فول و بک آپ گرفته نشد " + ErrorMessage)
                SaveTextExeption("اررور در انجام تسک بک آپ گیری، ده بار تلاش برای بک آپ گیری فول و بک آپ گرفته نشد " + ErrorMessage)
                Return "Error : " + ErrorMessage
            End If



            'SaveTextExeption("پایان بکاپ گیری")
            SaveBackupLogs(0, "پایان بک آپ گیری فول")
            Return "Complete"

        Catch ex As Exception
            SaveTextExeption("Error 27 : " + vbCrLf + "اررور در انجام تسک های اسکیوال برای بک آپ گیری" + vbCrLf + ex.Message.ToString())
            SaveBackupLogs(10, "اررور در انجام تسک بک آپ فول" + ex.Message.ToString())
            Throw ex
            Return "Error In Backup Task _ " + ex.Message.ToString()
        Finally
            _MyDb.CloseDB()
        End Try
        Return "Error In Backup Tasks "
    End Function

    Public Function DiffAdvance(ByVal DBList_Name As String, ByVal BackupPath As String, _Tmpdb As Parsic.DataAccess.DBase) As String

        If DoingFull = True Then
            SaveBackupLogs(0, "بک آپ فول در حال گرفتن است، به همین دلیل بک آپ دیف کنسل شد")
            Return "FinishT FullBackup is Taking, Diff Canceled"
        End If

        Dim ___MyDb As New Parsic.DataAccess.DBase(_Tmpdb.m_constr)

        Try
            'SaveTextExeption("شروع بکاپ گیری دیفرنشیال")
            SaveBackupLogs(0, "در حال تلاش برای گرفتن دیفرنشیال")
            Dim DateInfo As String = ""
            Dim LogInfo As String = ""

            '====================================================================================
            Threading.Thread.Sleep(250)
            Try
                ___MyDb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL, INIT, CHECKSUM, NOFORMAT, COMPRESSION", DataAccess.DBase.ExecuteMode._NONQEURY, 900)
                SaveBackupLogs(0, "دیفرنشیال با موفقیت گرفته شد")
                ErrorInDiff = False
            Catch ex As Exception
                If ex.Message.ToString.Contains("COMPRESSION") Then
                    SaveTextExeption("Error 29 : " + vbCrLf + "اررور در بک آپ گیری دیفرنشیال با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())
                    SaveBackupLogs(0, "اررور در بک آپ گیری دیفرنشیال با فشرده سازی، در حال بک آپ گیری بدون فشرده سازی " + vbCrLf + ex.Message.ToString())
                    ___MyDb.EXECmd("backup database [" & DBList_Name & "] to disk = '" & BackupPath & "' WITH DIFFERENTIAL, INIT, CHECKSUM, NOFORMAT", DataAccess.DBase.ExecuteMode._NONQEURY, 900)
                    SaveBackupLogs(0, "دیفرنشیال با موفقیت گرفته شد")
                    ErrorInDiff = False
                Else
                    SaveTextExeption("Error 30 : " + vbCrLf + "اررور در انجام تسک بک آپ گیری دیف اسکیوال " + ex.Message.ToString())
                    SaveBackupLogs(0, "اررور در انجام تسک بک آپ گیری دیف در اسکیوال " + ex.Message.ToString())
                    Return "Error"
                End If
            End Try

            'SaveTextExeption("پایان بکاپ گیری دیفرنشیال")
            SaveBackupLogs(0, "پایان بکاپ گیری دیفرنشیال")
            Return "Complete"

        Catch ex As Exception
            SaveTextExeption("Error 31 : " + vbCrLf + "اررور در انجام تسک های اسکیوال برای بک آپ گیری دیف" + vbCrLf + ex.Message.ToString())
            SaveBackupLogs(0, "اررور در انجام تسک های اسکیوال برای بک آپ گیری دیف" + vbCrLf + ex.Message.ToString())
            Return "Error In Backup Task"
        Finally
            ___MyDb.CloseDB()
        End Try
        Return "Error In Backup Tasks "
    End Function

    Public Function CreateZipFile(ByVal sourcePath As String, ByVal targetPath As String) As String
        Try
            ZipFile.CreateFromDirectory(sourcePath, targetPath, CompressionLevel.Optimal, False)
            SaveBackupLogs(13, "فایل زیپ بک آپ ساخته شد")
            Return "Ok"
        Catch ex As Exception
            SaveTextExeption("Error 28 : In ZipDirectory : " + ex.Message.ToString())
            SaveBackupLogs(12, "اررور در تبدیل بک آپ به فایل زیپ")
        End Try
        Return ""
    End Function

    Public Function GetDay()
        Dim MyNowDay As String = Date.Now.ToLongDateString()
        Dim Day As String = "Error"
        Try

            If MyNowDay.Contains("Saturday") Then
                Day = "Saturday"
            ElseIf MyNowDay.Contains("Sunday") Then
                Day = "Sunday"
            ElseIf MyNowDay.Contains("Monday") Then
                Day = "Monday"
            ElseIf MyNowDay.Contains("Tuesday") Then
                Day = "Tuesday"
            ElseIf MyNowDay.Contains("Wednesday") Then
                Day = "Wednesday"
            ElseIf MyNowDay.Contains("Thursday") Then
                Day = "Thursday"
            ElseIf MyNowDay.Contains("Friday") Then
                Day = "Friday"
            End If
        Catch ex As Exception
            SaveTextExeption("Error 06 : " + vbCrLf + "لطفا تاریخ سیستم را به صورت زیر تنظیم نمایید و ساعت را به صورت 12 ساعت تنظیم نمایید" & vbCrLf & "Region -> Additional Settings -> Date -> LongDate -> dddd, MMMM d, yyyy" & vbCrLf & ex.Message.ToString())
            ErrorLog = "لطفا تاریخ سیستم را به صورت زیر تنظیم نمایید و ساعت را به صورت 12 ساعت تنظیم نمایید" & vbCrLf & "Region -> Additional Settings -> Date -> LongDate -> dddd, MMMM d, yyyy" & vbCrLf & ex.Message.ToString()
            'Return "Error : System time is wrong"
        End Try
        Return Day
    End Function
    Public Function GetHour(Time As String) As Int32
        Try
            Dim s() As String
            s = Time.Split(":")
            Return s(0)
        Catch ex As Exception
            SaveTextExeption("Error 07 : " + ex.Message.ToString())
        End Try
        Return 0
    End Function
    Public Function GetMin(Time As String) As Int32
        Try
            Dim s() As String
            s = Time.Split(":")
            Return s(1)
        Catch ex As Exception
            SaveTextExeption("Error 08 : " + ex.Message.ToString())
        End Try
        Return 0
    End Function

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

    Public Function ChekLastBackupIsTrue(DBName As String, _Tmpdb As Parsic.DataAccess.DBase)
        Try
            SaveBackupLogs(0, "چک کردن آخرین بک آپ برای تطابق با دیفرنشیال")
            Dim LocalLastBackupTime As String
            Dim LastCloudBackupTime As String
            Try
                LocalLastBackupTime = LastBackupInLocalDB("Full", DBName, _Tmpdb).Rows(0)("backup_start_date").ToString()
            Catch ex As Exception
                Return False
                LocalLastBackupTime = ""
            End Try
            Dim dt As New DataTable("Log")

            _Tmpdb.dt_filler(dt, "execute SP_Get_Full_Diff_BackupLogs  @Type = 'Full' , @Count = 2 , @DBName='" & DBName & "'", CommandType.Text)
            'dt = PWS.Get_Full_Diff_BackupLogs(MyPublic.Get_UserName, MyPublic.Get_Password, LabID, "Full", 2)
            Try
                LastCloudBackupTime = dt.Rows(0)("Str_Backup_start_date_for_check").ToString()
            Catch ex As Exception
                Return False
                LastCloudBackupTime = ""
            End Try

            If Convert.ToDateTime(LocalLastBackupTime) = Convert.ToDateTime(LastCloudBackupTime) Then

                SaveBackupLogs(0, "بک آپ دیفرنشیال درست میباشد")
                Return True
            Else
                SaveTextExeption("Error 9.5 : Different Time,  Last Backup Time = " + Convert.ToDateTime(LocalLastBackupTime).ToString() + "  ,  Saved Time : " + Convert.ToDateTime(LastCloudBackupTime).ToString() + " ")
                SaveBackupLogs(0, "بک آپ گرفته شده با بک آپ دیفرنشیال یکی نمیباشد")
                Return False

            End If

            'If (LocalLastBackupTime = LastCloudBackupTime) Then
            '    Return True
            'Else
            '    Return False
            'End If
            'Chek 
        Catch ex As Exception
            SaveTextExeption("Error 10 : " + ex.Message.ToString())
            SaveBackupLogs(0, "اررور در چک کردن آخرین فول بک آپ برای دیفرنشیال")
            Return True
        End Try

    End Function

    Public Sub CheckDefaultInfo(DbNames As String, _Tmpdb As Parsic.DataAccess.DBase)

        Dim strIPAddress As String = ""
        Dim IISNetworkPath As String = ""
        Dim dt As New DataTable
        Dim strHostName As String
        Try

            strHostName = System.Net.Dns.GetHostName()
            strIPAddress = System.Net.Dns.GetHostByName(strHostName).AddressList(0).ToString()
            IISNetworkPath = "\\" & strIPAddress & "\ParsicTemp"
            _Tmpdb.dt_filler(dt, "select * from Tbl_BackupSchedule", CommandType.Text)
            'If dt.Rows.Count = 0 Then
            '    ErrorInfoLog = "هیچ تنظیمی برای بک آپ گیری اعمال نشده است"
            'End If
        Catch ex As Exception
            SaveBackupLogs(0, "اررور در فیلتر اطلاعات برنامه ریزی بک آپ، دقت داشته باشید آدرس پوشه برای بک آپ با آی پی ذخیره شده باشد  : " + vbCrLf + "strIPAddress : " + strIPAddress + vbCrLf + "IISNetworkPath  :" + IISNetworkPath + vbCrLf)
        End Try
        Try
            If Convert.ToInt32(dt.Rows.Count()) = 2 Then
                For i As Int16 = 0 To dt.Rows.Count() - 1
                    If dt.Rows(i)("Str_DbNames") = "Nothing" And dt.Rows(i)("Str_IISNetworkPath") = "Nothing" Then
                        _Tmpdb.EXECmd("Update Tbl_BackupSchedule set Str_DbNames = '" & DbNames & "', Str_IISNetworkPath = '" + IISNetworkPath + "' where Prk_BackupSchedule_AutoID = " & dt.Rows(i)("Prk_BackupSchedule_AutoID"), DataAccess.DBase.ExecuteMode._NONQEURY, 900)
                        SaveTextExeption("به صورت اتوماتیک تسک ها با اطلاعات زیر بروز شد : " + vbCrLf + "DbName : " + DbNames + vbCrLf + "Network Path :" + IISNetworkPath + vbCrLf)
                        SaveBackupLogs(0, "به صورت اتوماتیک تسک ها با اطلاعات زیر بروز شد : " + vbCrLf + "DbName : " + DbNames + vbCrLf + "Network Path :" + IISNetworkPath + vbCrLf)
                    End If
                Next

            End If
        Catch ex As Exception
            SaveBackupLogs(0, "اررور در ویرایش و ست کردن اطلاعات بک آپ در صورت اولین بار بودن از حالت ناتینگ به اطلاعات شبکه، دقت داشته باشید آدرس پوشه برای بک آپ با آی پی ذخیره شده باشد  : " + vbCrLf + "strIPAddress : " + strIPAddress + vbCrLf + "IISNetworkPath  :" + IISNetworkPath + vbCrLf)
        End Try

    End Sub

    Public Function ChekLastBackupForThreeDaysIsOk(DBName As String, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        Try
            Return True
            If GetDay() <> "Friday" Then
                Return True
            End If
            Dim daydiff As Int16 = 0
            Dim dt As New DataTable("Log")

            'If GetDay() <> "Wednesday" And GetDay() <> "Thursday" And GetDay() <> "Friday" Then
            '    Return True
            'End If

            _Tmpdb.dt_filler(dt, "select top 1 DATEDIFF(day, Str_Backup_start_date_for_check,dbo.GetNowDate()) as DAYDIFF from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Full' And Str_DBName = '" & DBName & "' order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)

            Try
                daydiff = Convert.ToInt16(dt.Rows(0)("DAYDIFF"))
                If daydiff >= 1 Then
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return True
            End Try

            'Chek 
        Catch ex As Exception
            SaveTextExeption("Error 10.2 : " + ex.Message.ToString())
            Return True
        End Try

    End Function


    Public Function ChekTimeForIISandSQL(_Tmpdb As Parsic.DataAccess.DBase) As String
        Try
            Dim curTimeZone As TimeZone = TimeZone.CurrentTimeZone
            Dim IISTimezone As String = curTimeZone.StandardName

            Dim SqlComand As String = "DECLARE @TimeZone VARCHAR(50);EXEC MASTER.dbo.xp_regread 'HKEY_LOCAL_MACHINE','SYSTEM\CurrentControlSet\Control\TimeZoneInformation','TimeZoneKeyName',@TimeZone OUT;SELECT @TimeZone as 'TimeZone'"
            Dim SQLTIMEZONE As New DataTable()
            _Tmpdb.dt_filler(SQLTIMEZONE, SqlComand, CommandType.Text)
            'SQLTIMEZONE = ExecuteCommand_DataTable(SqlComand)
            Dim SqlTimezon As String = SQLTIMEZONE.Rows(0)(0).ToString()

            If (IISTimezone = SqlTimezon) Then

                Dim IIShour As String = Now.Hour
                Dim IISminute As String = Now.Minute

                Dim IISDate As String = Now.Year.ToString() + "-" + Now.Month.ToString() + "-" + Now.Day.ToString()

                Dim Query As String = "select CONVERT(VARCHAR(4), year(GETDATE())) +'-'+ CONVERT(VARCHAR(2), month(GETDATE()))  +'-'+ CONVERT(VARCHAR(2), day(GETDATE())) as 'Date'  , datepart(hour, getdate()) as 'Hour' , datepart(minute, getdate()) as 'Minute'"
                Dim DTSQLTIME As New DataTable()
                _Tmpdb.dt_filler(DTSQLTIME, Query, CommandType.Text)
                'DTSQLTIME = ExecuteCommand_DataTable(Query)
                Dim SqlDate As String = DTSQLTIME.Rows(0)(0).ToString()
                Dim SqlHour As String = DTSQLTIME.Rows(0)(1).ToString()
                Dim SqlMinute As String = DTSQLTIME.Rows(0)(2).ToString()

                If (IISDate = SqlDate) Then
                    If (IIShour = SqlHour) Then
                        If (Convert.ToInt32(IISminute) = Convert.ToInt32(SqlMinute) Or Convert.ToInt32(IISminute) = Convert.ToInt32(SqlMinute) + 2 Or Convert.ToInt32(IISminute) = Convert.ToInt32(SqlMinute) - 2) Then
                            Return "OK"
                        Else
                            Return "تایم در آی آی اس و اس کیوال اختلاف دقیقه ای دارند، لطفا چک شود"
                        End If
                    Else
                        Return "تایم در آی آی اس و اس کیوال اختلاف ساعتی دارند، لطفا چک شود"
                    End If


                Else
                    Return "تایم در آی آی اس و اس کیوال اختلاف روز دارند، لطفا چک شود"
                End If
            Else
                Return "تایم در آی آی اس و اس کیوال اختلاف تایم زون دارند، لطفا چک شود"
            End If

            ' _Tmpdb.dt_filler(dt, "select top 1 DATEDIFF(day, Str_Backup_start_date_for_check,dbo.GetNowDate()) as DAYDIFF from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Full' And Str_DBName = '" & DBName & "' order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)

            Return "Nothing"

            'Chek 
        Catch ex As Exception
            SaveTextExeption("ارور در چک کردن تایم های IIS و SQL : " + ex.Message.ToString())
            Return "اررور اگستنشن : " + ex.Message.ToString()
        End Try

    End Function


    Public Function LastBackupInLocalDB(Type As String, DbName As String, _Tmpdb As Parsic.DataAccess.DBase) As DataTable
        Dim dt As New DataTable("DT")

        Dim CommandText As String = ""
        If Type = "Full" Then
            CommandText = "select top 1 * FROM msdb.dbo.backupset s inner join msdb.dbo.backupmediafamily m ON s.media_set_id = m.media_set_id WHERE s.database_name = '" & DbName & "' and s.type = 'D' and is_copy_only = 0 order by backup_set_id desc"
        ElseIf Type = "Diff" Then
            CommandText = "select top 1 * FROM msdb.dbo.backupset s inner join msdb.dbo.backupmediafamily m ON s.media_set_id = m.media_set_id WHERE s.database_name = '" & DbName & "' and s.type = 'I' order by backup_set_id desc"
        End If

        _Tmpdb.dt_filler(dt, CommandText, CommandType.Text)

        Return dt
    End Function

    Public Sub SaveTextExeption(ByVal Text As String)
        'Try
        '    Text = DateTime.Now & vbCrLf & "----------------------------------------------------------------" & vbCrLf & Text & vbCrLf & vbCrLf
        '    File.AppendAllText("C:\ParsicWebTemp\AutoBackupErrorLog.txt", Text & Environment.NewLine)
        '    Return True
        'Catch EX As Exception
        '    Return False
        'End Try

        Try
            Text = DateTime.Now & vbCrLf & "----------------------------------------------------------------" & vbCrLf & Text & vbCrLf & vbCrLf
            My.Computer.FileSystem.WriteAllText("C:\ParsicWebTemp\AutoBackupErrorLog.txt", Text, True)

        Catch ex As Exception
        End Try
    End Sub

    Public Function Get_UserName() As String
        ' Return "hbb" & Now.Year & "parsic" & Now.Month
        Return "Ticketing"
    End Function

    Public Function Get_Password() As String

        Try
            Dim MyKey As String = "**********************************************************************************************************************************"
            Dim MyTime As DateTime = Now
            Dim MyDate As String = "*****"+"*****"
            Dim MyPass As String = "*****" + "*****"

            MyPass = MySecurity.EncryptData(MyPass, MyKey)

            Return MyPass
        Catch ex As Exception
            Return ""
        End Try

    End Function











    Public Sub SaveBackupLogs(ByVal Order As Integer, ByVal Message As String)
        Dim __MyDb As New Parsic.DataAccess.DBase(tmpDb.m_constr)
        Try
            For i As Int16 = 0 To 3
                Try
                    __MyDb.EXECmd("execute SP_Insert_AutoBackupMessageLogs @Int_Order = " + Order.ToString() + ", @Str_Message = N'" + Message.ToString() + "'")
                    Exit For
                Catch ex As Exception
                    Thread.Sleep(2000)
                End Try
            Next

            __MyDb.CloseDB()
        Catch ex As Exception
            __MyDb.CloseDB()
            SaveTextExeption("Error 99 : اررور در ذخیره لاگ در بانک " + ex.Message.ToString())
        End Try

    End Sub


#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═══════════════════ SendToFTP ════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "SendToFTP"
    Public Function TransferToFTP(_Tmpdb As Parsic.DataAccess.DBase) As String
        Dim FullErrorCount As Int16 = 0
        Dim DiffErrorCount As Int16 = 0
        SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", 0, 0, "شروع کلی تابع ارسال بک آپ", _Tmpdb)
        Try
            _Tmpdb.EXECmd("truncate table Tbl_Full_Diff_BackupSubLogs")
        Catch ex As Exception
        End Try
        While StopRecivingProcess = False
            Try

                If ErrorInfoLog.Contains("هیچ تنظیماتی برای بک آپ گیری") Then
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", 0, 1000, "هیچ تنظیماتی برای بک آپ گیری اتوماتیک ذخیره نشده است", _Tmpdb)
                End If

                For i As Int16 = 0 To DTParsicMaster.Rows.Count() - 1
                    Dim MyMessage As String = ""
                    If DTParsicMaster.Rows(i)("Is_Present") Then

                        Dim FullDT As New DataTable()
                        Dim DiffDT As New DataTable()
                        _Tmpdb.dt_filler(FullDT, "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Full' and Bit_TransferToFTP=1 and Str_DBName = '" & DTParsicMaster.Rows(i)("DBList_Name") & "' order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)
                        _Tmpdb.dt_filler(DiffDT, "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Diff' and Bit_TransferToFTP=1 and Str_DBName = '" & DTParsicMaster.Rows(i)("DBList_Name") & "' order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)
                        Try
                            SendToFTPOrder = 0
                            If DiffDT.Rows(0)("Bit_TransferToFTP") And DTParsicMaster.Rows(i)("Is_Present") Then
                                SendToFTPOrder = SendToFTPOrder + 1
                                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "START", "START", 0, 1000, "شروع ارسال دیفرنشیال", _Tmpdb)
                                '_Tmpdb.EXECmd("truncate Table Tbl_Full_Diff_BackupSubLogs")
                                SendToFTPOrder = SendToFTPOrder + 1
                                SaveSubErrorLogInDB(0, Convert.ToInt32(DiffDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DiffDT.Rows(0)("Frk_Repository"), DiffDT.Rows(0)("Str_DBName"), DiffDT.Rows(0)("Str_BackupType"), DiffDT.Rows(0)("Str_DBBackupPath"), "", DiffDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "شروع ارسال دیفرنشیال به اف تی پی", _Tmpdb)

                                For j As Int16 = 0 To 3
                                    Dim ans As String = PreaperToSendToFTP(DiffDT, DTParsicMaster.Rows(i)("DBList_Name"), _Tmpdb)
                                    If ans.ToLower().Contains("ok") Then
                                        DiffErrorCount = 0
                                        Exit For
                                    Else
                                        DiffErrorCount += 1
                                    End If
                                    IsFirstPackajeForSane = True
                                Next
                                _Tmpdb.EXECmd("update Tbl_Full_Diff_BackupLogs set Bit_TransferToFTP = 0, Str_Description = '' where Prk_FullDiffLabsBackupLogs_AutoID = " & DiffDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID") & "")

                                If DiffErrorCount >= 2 Then
                                    SendToFTPOrder = SendToFTPOrder + 1
                                    SaveSubErrorLogInDB(0, Convert.ToInt32(DiffDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DiffDT.Rows(0)("Frk_Repository"), DiffDT.Rows(0)("Str_DBName"), DiffDT.Rows(0)("Str_BackupType"), DiffDT.Rows(0)("Str_DBBackupPath"), "", DiffDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "پایان ارسال دیفرنشیال به اف تی پی، ارسال انجام نشد", _Tmpdb)

                                Else
                                    SendToFTPOrder = SendToFTPOrder + 1
                                    SaveSubErrorLogInDB(0, Convert.ToInt32(DiffDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DiffDT.Rows(0)("Frk_Repository"), DiffDT.Rows(0)("Str_DBName"), DiffDT.Rows(0)("Str_BackupType"), DiffDT.Rows(0)("Str_DBBackupPath"), "", DiffDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "پایان ارسال دیفرنشیال به اف تی پی", _Tmpdb)

                                End If
                                SendToFTPOrder = SendToFTPOrder + 1
                                SaveSubErrorLogInDB(0, 0, 0, "", "ALL", "", "FINISH", "", SendToFTPOrder, 1000, "پایان ارسال دیفرنشیال", _Tmpdb)
                            End If
                        Catch ex As Exception
                            'SaveTextExeption("Error 40 : In transfer : " + ex.Message.ToString())
                        End Try
                        Try
                            If FullDT.Rows(0)("Bit_TransferToFTP") And DTParsicMaster.Rows(i)("Is_Present") Then
                                SendToFTPOrder = SendToFTPOrder + 1
                                SaveSubErrorLogInDB(0, Convert.ToInt32(FullDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), FullDT.Rows(0)("Frk_Repository"), FullDT.Rows(0)("Str_DBName"), FullDT.Rows(0)("Str_BackupType"), FullDT.Rows(0)("Str_DBBackupPath"), "", FullDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "شروع ارسال فول به اف تی پی", _Tmpdb)
                                For j As Int16 = 0 To 3
                                    Dim ans As String = PreaperToSendToFTP(FullDT, DTParsicMaster.Rows(i)("DBList_Name"), _Tmpdb)
                                    If ans.ToLower().Contains("ok") Then
                                        FullErrorCount = 0
                                        Exit For
                                    Else
                                        FullErrorCount += 1
                                    End If
                                    IsFirstPackajeForSane = True
                                Next
                                _Tmpdb.EXECmd("update Tbl_Full_Diff_BackupLogs set Bit_TransferToFTP = 0, Str_Description = ''  where Prk_FullDiffLabsBackupLogs_AutoID = " & FullDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID") & "")

                                If FullErrorCount >= 2 Then
                                    SendToFTPOrder = SendToFTPOrder + 1
                                    SaveSubErrorLogInDB(0, Convert.ToInt32(FullDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), FullDT.Rows(0)("Frk_Repository"), FullDT.Rows(0)("Str_DBName"), FullDT.Rows(0)("Str_BackupType"), FullDT.Rows(0)("Str_DBBackupPath"), "", FullDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "پایان ارسال فول به اف تی پی، ارسال انجام نشد", _Tmpdb)

                                Else
                                    SendToFTPOrder = SendToFTPOrder + 1
                                    SaveSubErrorLogInDB(0, Convert.ToInt32(FullDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), FullDT.Rows(0)("Frk_Repository"), FullDT.Rows(0)("Str_DBName"), FullDT.Rows(0)("Str_BackupType"), FullDT.Rows(0)("Str_DBBackupPath"), "", FullDT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "پایان ارسال فول به اف تی پی", _Tmpdb)

                                End If
                            End If
                        Catch ex As Exception
                            'SaveTextExeption("Error 41 : In transfer : " + ex.Message.ToString())
                        End Try
                        'If CheckBackup(_Tmpdb, MyMessage) Then
                        'Else
                        '    'Error In Restore Backup
                        'End If
                    End If
                Next



            Catch ex As Exception
                SaveTextExeption("Error 42 : In transfer : " + ex.Message.ToString())
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "FINISH", "", 0, 1000, "ارور در ارسال کلی برای یک بانک", _Tmpdb)
            End Try

            'Try
            '    If ErrorCount >= 6 Then
            '        For i As Int16 = 0 To DTParsicMaster.Rows.Count() - 1
            '            Dim FullDT As New DataTable()
            '            Dim DiffDT As New DataTable()
            '            _Tmpdb.dt_filler(FullDT, "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Full' and Str_DBName = '" & DTParsicMaster.Rows(i)("DBList_Name") & "' And Int_Status=1 And Str_BackupSize> Cast(0 as bigint) order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)

            '            _Tmpdb.dt_filler(DiffDT, "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Diff' and Str_DBName = '" & DTParsicMaster.Rows(i)("DBList_Name") & "' And Int_Status=1 And Str_BackupSize> Cast(0 as bigint)  order by Prk_FullDiffLabsBackupLogs_AutoID desc", CommandType.Text)
            '            _Tmpdb.EXECmd("update Tbl_Full_Diff_BackupLogs set Bit_TransferToFTP = 0 where Prk_FullDiffLabsBackupLogs_AutoID = " & DiffDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID") & "")
            '            _Tmpdb.EXECmd("update Tbl_Full_Diff_BackupLogs set Bit_TransferToFTP = 0 where Prk_FullDiffLabsBackupLogs_AutoID = " & FullDT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID") & "")
            '            SendToFTPOrder = SendToFTPOrder + 1
            '            SaveSubErrorLogInDB(0, 0, 0, DTParsicMaster.Rows(i)("DBList_Name"), "Full", "", "FINISH", "", 0, 1000, "ارور در ارسال کلی ، کل", _Tmpdb)

            '        Next
            '        ErrorCount = 0
            '    End If
            'Catch ex As Exception
            '    SaveTextExeption("Error 43 : In transfer : " + ex.Message.ToString())
            'End Try

            Thread.Sleep(2 * 60 * 1000)
        End While

        'Dim ans As String = SplitAndSendFile(NetworkDbBackupPath + "\" + DbBackupName, DbBackupName, "FTPBackupSubPath", "_FTPServerPath", "_FTPUsername", "_FTPPassword")
        Return ""
    End Function

    Public Function PreaperToSendToFTP(DT As DataTable, DbName As String, _Tmpdb As Parsic.DataAccess.DBase) As String
        Try
            Dim BackupRepositorySchedule As New DataTable()
            BackupRepositorySchedule = PWS.Get_LabBackupRepositorySchedule(Get_UserName, Get_Password, LabID)
            Dim Repository As Int64 = BackupRepositorySchedule.Rows(0)("Prk_LabBackupRepository_AutoID")
            Dim Type As String = DT.Rows(0)("Str_BackupType")
            Dim Size As String = DT.Rows(0)("Str_BackupSize")
            Dim Desc As String = DT.Rows(0)("Str_Description")
            Dim ChekSum As String = DT.Rows(0)("Str_ChkSum")
            Dim LastReception As Integer = DT.Rows(0)("Int_LastReception")
            Dim Backup_start_date_for_check As String = DT.Rows(0)("Str_Backup_start_date_for_check")
            Dim IISBackupPath As String = DT.Rows(0)("Str_DBBackupPath")

            Dim MyFullDiffLabsBackupLogsID As Integer = -1
            MyFullDiffLabsBackupLogsID = PWS.Set_Full_Diff_BackupLogs(Get_UserName, Get_Password, LabID, Repository, DbName, Type, Size, ChekSum, LastReception, Backup_start_date_for_check, IISBackupPath, "", "", "شروع ارسال", 0)
            Dim FilePath As String = IISBackupPath
            Dim FileName() As String = FilePath.Split("\")
            Dim FTPName As String = BackupRepositorySchedule.Rows(0)("Str_Name")
            Dim FTPBackupSubPath As String = "LabBackups/" + LabID.ToString() + "/" + DT.Rows(0)("Str_DBName") + "/" + Now.Year.ToString() + "_" + Now.Month.ToString() + "_" + Now.Day.ToString() + "/"
            Dim FtpServerPath As String = BackupRepositorySchedule.Rows(0)("Str_FTPPath")  ' "ftp://81.16.116.84/" '  
            Dim FtpUsername As String = BackupRepositorySchedule.Rows(0)("Str_FTPUsername")  ' "Administrator" ' 
            Dim FtpPassword As String = BackupRepositorySchedule.Rows(0)("Str_FTPPassword")  '"**********" '
            Dim SizeOfEachFiles As Long = 10485760  '  1048576   ' 20971520  '


            SaveTextExeption("ftpAddress : " + FtpServerPath + vbCrLf + "ftpUser : " + FtpUsername + vbCrLf + "ftpPassword : ********* " + vbCrLf + "FTPBackupSubPath = " + FTPBackupSubPath + vbCrLf + "SizeOfEachFiles : " + SizeOfEachFiles.ToString())

            If Convert.ToUInt64(Size) > Convert.ToUInt64(1073741824) And Desc <> "Proxy" Then
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "حجم فایل بک آپ پشتیبان بیشتر از یک گیگ می باشد", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "حجم فایل بک آپ پشتیبان بیشتر از یک گیگ می باشد", _Tmpdb)
                PWS.Edit_Full_Diff_BackupLogs(Get_UserName, Get_Password, MyFullDiffLabsBackupLogsID, FTPBackupSubPath + FileName(FileName.Count() - 1), False, FtpServerPath, "", "حجم فایل بک آپ پشتیبان بیشتر از یک گیگ می باشد", 0)
                Return "Error Size:More Than 1GB , ok"
            End If



            'If (CheckBak(IISBackupPath, _Tmpdb)) Then
            'Else
            '    Return "در چک کردن بک آپ خطای وجود دارد"
            'End If


            Dim Str_IP As String = FtpServerPath.Replace("ftp://", "").Replace("/", "")

            Dim ping As Boolean = Ping_Ip_Port(Str_IP, 21)

            If ping = False Then
                SaveTextExeption(" آی پی شرکت و یا پرت 21 در دسترس نمیباشد")
                Return " آی پی شرکت و یا پرت 21 در دسترس نمیباشد"
            End If


            SaveTextExeption("شروع آماده کردن اف تی پی")

            Assembler = New FTPAssembler.WebService1

            'Dim Str_Url As String = "http://" & Str_IP & ":8595/Service.asmx"
            Dim Str_Url As String = "http://" & Str_IP & ":8595/WebService1.asmx"




            Dim Url As New ServiceModel.EndpointAddress(Str_Url)
            Assembler.Url = Str_Url

            SaveTextExeption("Connected to " & Str_Url)
            'SendToFTPOrder = SendToFTPOrder + 1
            'SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "دستور العمل ابری دریافت شد" + "     " + "تعداد دستور العمل = " + BackupRepositorySchedule.Rows.Count().ToString(), _Tmpdb)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "سایز : " + (Size / 1024 / 1024).ToString() + "MB", _Tmpdb)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "ارسال به : " + FTPName.ToString() + "     " + FtpServerPath + "     URL : " + Str_Url, _Tmpdb)

            Dim ans As String = SplitAndSendFile(DT, FilePath, FileName(FileName.Count() - 1), FTPBackupSubPath, FtpServerPath, FtpUsername, FtpPassword, _Tmpdb, SizeOfEachFiles)
            Try
                If ans.ToLower.Contains("ok") Then
                    PWS.Edit_Full_Diff_BackupLogs(Get_UserName, Get_Password, MyFullDiffLabsBackupLogsID, FTPBackupSubPath + FileName(FileName.Count() - 1), True, FtpServerPath, "Transfer Finished", "", 1)
                Else
                    PWS.Edit_Full_Diff_BackupLogs(Get_UserName, Get_Password, MyFullDiffLabsBackupLogsID, FTPBackupSubPath + FileName(FileName.Count() - 1), False, FtpServerPath, "", "Error : " + ans, 0)
                End If
            Catch ex As Exception
                SaveTextExeption("Error 43.5 : In Save Transfer Cloud Log : " + ex.Message.ToString())
            End Try
            Return ans
        Catch ex As Exception
            SaveTextExeption("Error 44 : In transfer : " + ex.Message.ToString())
            SaveTextExeption("اطلاعات آدرس FTP در سرور ابری یافت نشد لطفا اطلاعات را در تیکتینگ ثبت نمایید")
            Return ""
        End Try

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

    Public Function CheckBak(path As String, _Tmpdb As Parsic.DataAccess.DBase)
        Dim ans As Boolean = False
        SendToFTPOrder = SendToFTPOrder + 1
        SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, "شروع تست", _Tmpdb)
        Dim index As Int16 = 0
        Dim FolderPath As String = ""
        Dim MyBakPath As String = ""
        Try
            For j As Int16 = 0 To path.Length
                Dim c As Char = path(j)
                If c = "\" Then
                    index = j
                End If
            Next

        Catch ex As Exception

        End Try
        FolderPath = path.Substring(0, index + 1)


        Try
            Directory.CreateDirectory(FolderPath + "ForFtpTest")
        Catch ex As Exception
        End Try

        DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)


        Dim TOOO As Int32 = (Convert.ToInt32(path.Length) - 4) - (index + 1)
        Dim Fullname As String = path.Substring(index + 1, TOOO).Replace("bak", ".bak").Replace("dif", ".dif")

        If File.Exists(path) Then
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " شروع اگسترکت کردن فایل زیپ بک آپ ", _Tmpdb)
            ZipFile.ExtractToDirectory(path, FolderPath + "ForFtpTest")
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " پایان اگسترکت کردن فایل زیپ یک آپ ", _Tmpdb)
        End If
        MyBakPath = FolderPath + "ForFtpTest\" + Fullname


        Try
            _Tmpdb.EXECmd("RESTORE VERIFYONLY FROM DISK = '" + MyBakPath + "'", DataAccess.DBase.ExecuteMode._NONQEURY)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " فایل بک آپ درست میباشد ", _Tmpdb)
            ans = True
        Catch ex As Exception
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " فایل بک آپ خراب میباشد ", _Tmpdb)
            ans = False
        End Try


        Try
            DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)
            Dim del As String = FolderPath + "ForFtpTest"
            Directory.Delete(del)
        Catch ex As Exception
        End Try

        Return ans
    End Function

    Public Function SplitAndSendFile(ByVal DT As DataTable, ByVal filepath As String, ByVal BackUpName As String, ByVal FtpLocalPath As String, ByVal _FTPServerPath As String, ByVal _FTPUsername As String, ByVal _FTPPassword As String, _Tmpdb As Parsic.DataAccess.DBase, Optional ByVal SizeOfEachFiles As Long = 5242880) As String
        Try
            SaveTextExeption("شروع به آپلود فایل ")
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "شروع به آپلود فایل ", _Tmpdb)

            Dim NameOfSplite As String = "Split"
            Dim counter As Int16 = 1
            Dim ErrorCloudCount As Int32 = 0
            Dim _ans As String = ""
            Dim size As Long = SizeOfEachFiles
            Do
                Try
                    _ans = SplitFile(DT, filepath, size, _FTPServerPath, _FTPUsername, _FTPPassword, FtpLocalPath, NameOfSplite, _Tmpdb)
                Catch ex As Exception
                    SaveTextExeption("Error 44.5 : In SplitAndSendFile Function " + vbCrLf + ex.Message.ToString())
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "اررور کلی در تابع SplitAndSendFile " + vbCrLf + ex.Message.ToString(), _Tmpdb)
                End Try

                If _ans = "" Then
                    Return "تلاش فراوان و عدم ارسال"
                End If
                If _ans.Contains("Ok") Then
                    'Get Info From Lab

                    Dim sss As String

                    Try
                        SaveTextExeption("...FtpLocalPath : " + FtpLocalPath + "     ...BackUpName : " + BackUpName)
                        sss = Assembler.Combine_BackUp_Splits_On_FTP_Server(FtpLocalPath, BackUpName, "Split")
                        SaveTextExeption("Transfer answer : " + sss)
                    Catch ex As Exception
                        SaveTextExeption("Error 44.6 :  In assembler Function " + vbCrLf + ex.Message.ToString())
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "اررور کلی در تابع assembler " + vbCrLf + ex.Message.ToString(), _Tmpdb)
                    End Try

                    If sss = "OK" Then
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "فایل به صورت کامل دریافت شد", _Tmpdb)
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "", _Tmpdb)
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "", _Tmpdb)
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "", _Tmpdb)
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "", _Tmpdb)
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "", _Tmpdb)

                        SaveTextExeption("عملیات با موفقیت به پایان رسید")
                        Return "ok"
                    Else
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است، لطفا ارتباط با اینترنت یا(آی آی اس) سرور اف تی پی در شرکت را بررسی نمایید", _Tmpdb)
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "Error EX : " + sss, _Tmpdb)

                        SaveTextExeption("فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است، لطفا ارتباط با اینترنت یا(آی آی اس) سرور اف تی پی در شرکت را بررسی نمایید" + vbCrLf + "Error : " + sss)
                        Return "فایل ها با موفقیت ارسال شده اما در سرهم کردن فایل های ارسال شده در سرور شرکت مشکلی بوجود آمده است، لطفا ارتباط با اینترنت یا(آی آی اس) سرور اف تی پی در شرکت را بررسی نمایید" + vbCrLf + "ERROR : " + sss
                    End If
                    'Get Info From Lab\
                ElseIf _ans = "ErrorSize" Then
                    SaveTextExeption("اررور در سرعت" + size.ToString() + "در حال تلاش با سرعت" + (size / 2).ToString())
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "اررور در سرعت" + size.ToString(), _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "در حال تلاش با سرعت     " + (size / 2).ToString(), _Tmpdb)

                    size = size / 2
                    If size < 262144 Then '1048576
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "سرعت آپلود بسیار پایین میباشد", _Tmpdb)
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "آخرین سرعت : " + (size * 2).ToString(), _Tmpdb)

                        Return "Error : سرعت آپلود بسیار پایین میباشد " + vbCrLf + "Last Spead : " + (size * 2).ToString()
                        Exit Do
                    End If
                ElseIf _ans = "در ارسال لاگ به سرور ابری مشکلی وجود دارد" Then
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "در ارسال لاگ به سرور ابری مشکلی وجود دارد", _Tmpdb)
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
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "اررور کلی در تابع SplitAndSendFile " + vbCrLf + ex.Message.ToString(), _Tmpdb)
            Return "Error In SplitAndSendFile Function " + vbCrLf + ex.Message.ToString()
        End Try

    End Function


    Private Function SplitFile(ByVal DT As DataTable, ByVal inputFileName As String, ByVal SizeOfEachFiles As Long, ByVal _FTPServerPath As String, ByVal _FTPUsername As String, ByVal _FTPPassword As String, ByVal FtpLocalPath As String, ByVal NameOfSplite As String, _Tmpdb As Parsic.DataAccess.DBase) As String
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
            SaveTextExeption("سایز کل :  " + (fileLength / 1024 / 1024).ToString() + "MB")
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, numberOfFiles, "تعداد کل بسته ها : " + numberOfFiles.ToString() + "          " + "سایز هر بسته : " + (SizeOfEachFiles / 1024 / 1024).ToString() + "MB", _Tmpdb)

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

                    myans = SendPackage(DT, buffer, _FTPServerPath, _FTPUsername, _FTPPassword, FtpLocalPath, NameOfSplite + fileCount.ToString() + ".bak", ErrInfo, _Tmpdb)

                    If myans Then
                        'SaveTextExeption("ارسال بسته شماره " + fileCount.ToString())
                        Exit While
                    End If
                    SaveTextExeption("اررور در ارسال بسته شماره " + fileCount.ToString() + vbCrLf + "Error Message : " + ErrInfo)

                    If ErrInfo.Contains("ErrorSize") Then
                        Return "ErrorSize"
                    End If

                    Thread.Sleep(10)
                    If (errorcounter >= 5) Then
                        SaveTextExeption("در ارسال اطلاعات خطایی رخ داده است و بیش از 5 بار تلاش ناموفق وجود دارد" + vbCrLf + "Error In Send File " + NameOfSplite + fileCount.ToString() + ".bak")
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, numberOfFiles, "بیش از 5 بار تلاش و عدم ارسال. ارسال لغو شد", _Tmpdb)

                        Return "در ارسال اطلاعات خطایی رخ داده است و بیش از 5 بار تلاش ناموفق وجود دارد" + vbCrLf + "Error In Send File " + NameOfSplite + fileCount.ToString() + ".bak" + vbCrLf + ErrInfo
                    End If
                    errorcounter += 1
                End While

                fileCount += 1
            Loop
            SendToFTPOrder = SendToFTPOrder + 1
        Catch ex As Exception
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "فایل بر روی شبکه قابل دست یابی نمیباشد ", _Tmpdb)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "Path : " + inputFileName, _Tmpdb)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "ERROR EX : " + ex.Message.ToString(), _Tmpdb)

            If ErrorFinder = 0 Then
                SaveTextExeption("Error 46 : " + vbCrLf + "فایل بر روی شبکه قابل دست یابی نمیباشد " + vbCrLf + inputFileName + vbCrLf + ex.Message.ToString())
                Return "فایل بر روی شبکه قابل دست یابی نمیباشد " + vbCrLf + inputFileName + vbCrLf + ex.Message.ToString()
            ElseIf ErrorFinder = 1 Then
                SaveTextExeption("Error 47 : " + vbCrLf + "در خواندن فایل بر روی سرور اسکیوال خطایی رخ داده است ارسال اطلاعات به اف تی پی سرور انجام نشد است " + vbCrLf + FtpLocalPath + NameOfSplite + fileCount.ToString() + ".bak" + vbCrLf + ex.Message.ToString() + vbCrLf + inputFileName)
                Return "در خواندن فایل بر روی سرور اسکیوال خطایی رخ داده است ارسال اطلاعات به اف تی پی سرور انجام نشد است " + vbCrLf + FtpLocalPath + NameOfSplite + fileCount.ToString() + ".bak" + vbCrLf + ex.Message.ToString() + vbCrLf + inputFileName
            End If
        End Try
        SendToFTPOrder = SendToFTPOrder + 1
        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 1000, "تمام بسته ها ارسال شد", _Tmpdb)
        Return "ارسال با موفقیت به اتمام رسید" + vbCrLf + "Ok"
    End Function



    Public Function SendPackage(ByVal DT As DataTable, ByVal Info As Byte(), ByVal FTPServerPath As String, ByVal FTPServerUsername As String, ByVal FTPServerPassword As String, ByVal _FTPBackupSubPath As String, ByVal BackupFileName As String, ByRef ErrorInfo As String, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        Try

            'FtpServer.Timeout = 1000 * 60
            'Dim Myerror As String = FtpServer.SaveSpleat("D:\Parsic_FTP\PWS_Backups\" + BackupFileName, Info)

            Dim err As Exception
            Return UploadFTPByte(DT, FTPServerPath, FTPServerUsername, FTPServerPassword, Info, _FTPBackupSubPath + BackupFileName, err, ErrorInfo, _Tmpdb)

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

    Public Function UploadFTPByte(ByVal DT As DataTable, ftpAddress As String, ftpUser As String, ftpPassword As String, buffer As Byte(), targetFileName As String, ExceptionInfo As Exception, ByRef ErrorInfo As String, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        'SaveTextExeption("ftpAddress : " + ftpAddress + vbCrLf + "ftpUser : " + ftpUser + vbCrLf + "ftpPassword : ********* " + vbCrLf + "targetFileName = " + targetFileName + vbCrLf + "buffer.count : " + buffer.Count().ToString())
        Dim credential As NetworkCredential
        Dim sFtpFile As String = ""
        ErrorInfo = ""
        Try
            'SendToFTPOrder = SendToFTPOrder + 1
            'SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "در حال ذخیره در اف تی پی ", _Tmpdb)
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "آدرس : " + targetFileName, _Tmpdb)

            credential = New NetworkCredential(ftpUser, ftpPassword)
            If ftpAddress.EndsWith("/") = False Then ftpAddress = ftpAddress & targetFileName & "/"
            sFtpFile = ftpAddress & targetFileName ' & fileToUpload
            Dim request As FtpWebRequest = DirectCast(WebRequest.Create(sFtpFile), FtpWebRequest)
            request.KeepAlive = False
            request.Method = WebRequestMethods.Ftp.UploadFile
            request.Credentials = credential
            request.UsePassive = False
            request.Timeout = (60 * 1000) * 3 '3 mins
            'SaveTextExeption("request.Timeout : " + request.Timeout.ToString())
            request.ContentLength = buffer.Length
            Dim stream As Stream = request.GetRequestStream

            stream.Write(buffer, 0, buffer.Length)

            stream.Close()

            Using response As FtpWebResponse = DirectCast(request.GetResponse, FtpWebResponse)

                response.Close()

            End Using
            'SendToFTPOrder = SendToFTPOrder + 1
            'SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ذخیره شد", _Tmpdb)

            Return True

        Catch ex As Exception
            SaveTextExeption("Error 50 : In Send To Ftp : " + ex.Message.ToString())
            SendToFTPOrder = SendToFTPOrder + 1

            If (IsFirstPackajeForSane) Then
                If ex.Message.ToString.Contains("file not found") Then
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "فولدر آزمایشگاه در سرور FTP یافت نشد، تلاش برای ساخت فولدر با نام آی دی آزمایشگاه", _Tmpdb)
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

                    FtpFolderCreate(NewLabDirectoryPath, ftpUser, ftpPassword, False, DT, _Tmpdb)
                    FtpFolderCreate(NewLabSubDirectoryPath, ftpUser, ftpPassword, True, DT, _Tmpdb)
                    FtpFolderCreate(LastPath, ftpUser, ftpPassword, True, DT, _Tmpdb)
                    IsFirstPackajeForSane = False
                ElseIf ex.Message.ToString.Contains("The underlying connection was closed: An unexpected error occurred on a receive.") Then
                    If targetFileName.Contains("Split1.") Then
                        ErrorInfo = "ErrorSize " + ex.Message.ToString()
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX : " + ex.Message.ToString(), _Tmpdb)

                        DeleteFTPFile(sFtpFile)
                    End If
                ElseIf ex.Message.ToString.Contains("The operation has timed out") Then
                    If targetFileName.Contains("Split1.") Then
                        ErrorInfo = "ErrorSize " + ex.Message.ToString()
                        DeleteFTPFile(sFtpFile)
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                        SendToFTPOrder = SendToFTPOrder + 1
                        SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX2 : " + ex.Message.ToString(), _Tmpdb)

                    End If
                Else
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX3 : " + ex.Message.ToString(), _Tmpdb)

                End If
            ElseIf ex.Message.ToString.Contains("The underlying connection was closed: An unexpected error occurred on a receive.") Then
                If targetFileName.Contains("Split1.") Then
                    ErrorInfo = "ErrorSize " + ex.Message.ToString()
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX : " + ex.Message.ToString(), _Tmpdb)

                End If
                DeleteFTPFile(sFtpFile)
            ElseIf ex.Message.ToString.Contains("The operation has timed out") Then
                If targetFileName.Contains("Split1.") Then
                    ErrorInfo = "ErrorSize " + ex.Message.ToString()
                    DeleteFTPFile(sFtpFile)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX2 : " + ex.Message.ToString(), _Tmpdb)

                End If
                'ElseIf ex.Message.Contains("Unable to connect to the remote server") Then
                '    If targetFileName.Contains("Split1") Then
                '        ErrorInfo = "ErrorSize"
                '    End If
                '    DeleteFTPFile(sFtpFile)

            Else
                SaveTextExeption("در ارسال اطلاعات به اف تی پی سرور خطایی رخ داده است" + vbCrLf + ex.Message.ToString())
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, " در ارسال فایل خطایی رخ داده است", _Tmpdb)
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX3 : " + ex.Message.ToString(), _Tmpdb)

                ErrorInfo = "Error In Transfer To FTP 1003 : " & ex.Message.ToString()
            End If

            ExceptionInfo = ex
            If ErrorInfo = "" Then
                ErrorInfo = "Err : " + ex.Message.ToString()
            End If

            Return False
        Finally

        End Try

    End Function

    Private Function FtpFolderCreate(folder_name As String, username As String, password As String, ISdayPath As Boolean, DT As DataTable, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        Try
            Dim request As Net.FtpWebRequest = CType(FtpWebRequest.Create(folder_name), FtpWebRequest)
            request.Credentials = New NetworkCredential(username, password)
            request.Method = WebRequestMethods.Ftp.MakeDirectory
            Try
                Using response As FtpWebResponse = DirectCast(request.GetResponse(), FtpWebResponse)
                    ' Folder created
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "فولدر ساخته شد ", _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "Address : " + folder_name, _Tmpdb)

                End Using
            Catch ex As WebException
                If ISdayPath Then
                    SaveTextExeption("در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است" + vbCrLf + folder_name + vbCrLf + ex.Message.ToString())
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است", _Tmpdb)
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR : " + ex.Message.ToString(), _Tmpdb)

                End If
                Dim response As FtpWebResponse = DirectCast(ex.Response, FtpWebResponse)
                ' an error occurred
                If response.StatusCode = FtpStatusCode.ActionNotTakenFileUnavailable Then
                End If
            End Try
            Return True
        Catch ex As Exception
            If ISdayPath Then
                SaveTextExeption("Error 49 : " + vbCrLf + "در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است" + vbCrLf + ex.Message.ToString())
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "در ساختن پوشه آزمایشگاه بر روی اف تی پی سرور خطایی رخ داده است", _Tmpdb)
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, Convert.ToInt32(DT.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID")), DT.Rows(0)("Frk_Repository"), DT.Rows(0)("Str_DBName"), DT.Rows(0)("Str_BackupType"), DT.Rows(0)("Str_DBBackupPath"), "", DT.Rows(0)("Str_FtpPath"), SendToFTPOrder, 10000, "ERROR EX : " + ex.Message.ToString(), _Tmpdb)

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

    Public Sub SaveSubErrorLogInDB(BackupScheduleID As Int32, FullDiffLabsBackupID As Int32, Repository As Int32, DBName As String, BackupType As String, BackupPath As String, ErrorLog As String, FtpPath As String, Order As Int32, WholeCount As Int32, Description As String, _Tmpdb As Parsic.DataAccess.DBase)
        Dim _MyDb As New Parsic.DataAccess.DBase(_Tmpdb.m_constr)
        Try

            Dim Com As String = "execute SP_Insert_Full_Diff_BackupSubLogs @Frk_FullDiffLabsBackupID = " + FullDiffLabsBackupID.ToString() + " ,@Frk_BackupScheduleID = " + BackupScheduleID.ToString() + ",@Frk_Repository = " + Repository.ToString() + ",@Str_DBName = N'" + DBName + "',@Str_BackupType = N'" + BackupType + "',@Str_BackupPath = N'" + BackupPath + "',@Str_ErrorLog = N'" + ErrorLog + "',@Str_FtpPath = N'" + FtpPath + "',@Int_Order = " + Order.ToString() + ",@Int_WholeCount = " + WholeCount.ToString() + ",@Str_Description = N'" + Description + "'"
            _MyDb.EXECmd(Com)
            _MyDb.CloseDB()
        Catch ex As Exception
            SaveTextExeption("Execute Error : " + ex.Message.ToString() + vbCrLf)
            _MyDb.CloseDB()
        End Try
    End Sub

    Public Function CheckBackup(_Tmpdb As Parsic.DataAccess.DBase, ByRef MyMessage As String) As Boolean
        SendToFTPOrder = SendToFTPOrder + 1
        SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, "شروع ریستور", _Tmpdb)
        Dim MdfName As String = ""
        Dim LdfName As String = ""
        Dim DBName As String = ""


        Dim DBFullPath As String = ""
        Dim DBDiffPath As String = ""
        Dim FolderPath As String = ""
        Dim LastAdmitID As Int64 = 0
        Dim dt As New DataTable("DT")
        Try


            dt = ExecDT("SELECT name AS logical_name FROM sys.database_files", _Tmpdb)
            MdfName = dt.Rows(0)("logical_name").ToString()
            LdfName = dt.Rows(1)("logical_name").ToString()

            Dim com As String = "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Full' and cast(Str_BackupSize as bigint) > cast(0 as bigint) and Int_Status = 1 order by Prk_FullDiffLabsBackupLogs_AutoID desc"
            dt.Clear()
            dt = ExecDT(com, _Tmpdb)
            If dt.Rows.Count = 0 Then
                MyMessage = "هیچ بک آپ درستی گرفته نشده است"
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, "هیچ بک آپی به درستی گرفته نشده است", _Tmpdb)
                Return False
            End If
            LastAdmitID = dt.Rows(0)("Int_LastReception")
            DBName = dt.Rows(0)("Str_DBName").ToString()
            DBFullPath = dt.Rows(0)("Str_DBBackupPath").ToString()
            com = "select top 1 * from Tbl_Full_Diff_BackupLogs where Str_BackupType = 'Diff' and cast(Str_BackupSize as bigint) > cast(0 as bigint) and Int_Status = 1 and Prk_FullDiffLabsBackupLogs_AutoID > " & dt.Rows(0)("Prk_FullDiffLabsBackupLogs_AutoID") & "  order by Prk_FullDiffLabsBackupLogs_AutoID desc"
            dt = ExecDT(com, _Tmpdb)
            If dt.Rows.Count <> 0 Then
                LastAdmitID = dt.Rows(0)("Int_LastReception")
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " آخرین شماره پذیرش " + LastAdmitID.ToString(), _Tmpdb)
            End If
            Try
                DBDiffPath = dt.Rows(0)("Str_DBBackupPath").ToString()
            Catch ex As Exception
                DBDiffPath = ""
            End Try
            Dim index As Int16 = 0
            Try
                For j As Int16 = 0 To DBFullPath.Length
                    Dim c As Char = DBFullPath(j)
                    If c = "\" Then
                        index = j
                    End If
                Next

            Catch ex As Exception

            End Try
            FolderPath = DBFullPath.Substring(0, index + 1)
            Try
                Dim M As String = ""
                If DropDB(DBName, M, _Tmpdb) = False Then
                    MyMessage = "در حذف دیتابیس تستی مشکلی وجود دارد" + vbCrLf + M
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " در حذف دیتابیس تستی مشکلی وجود دارد ", _Tmpdb)
                    Return False
                End If
                Directory.CreateDirectory(FolderPath + "ForFtpTest")
            Catch ex As Exception
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, "خطا در ساخت پوشه " & FolderPath & "ForFtpTest" & "      " & ex.Message, _Tmpdb)
                SaveTextExeption("خطا در ساخت پوشه " & FolderPath & "ForFtpTest" & vbCrLf & ex.Message)
            End Try

            DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)


            Dim TOOO As Int32 = (Convert.ToInt32(DBFullPath.Length) - 4) - (index + 1)
            Dim Fullname As String = DBFullPath.Substring(index + 1, TOOO).Replace("bak", ".bak")
            If File.Exists(DBFullPath) Then
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " شروع اگسترکت کردن فایل زیپ فول بک آپ ", _Tmpdb)

                ZipFile.ExtractToDirectory(DBFullPath, FolderPath + "ForFtpTest")

                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " پایان اگسترکت کردن فایل زیپ فول یک آپ ", _Tmpdb)
            End If
            DBFullPath = FolderPath + "ForFtpTest\" + Fullname

            index = 0
            Try
                For j As Int16 = 0 To DBDiffPath.Length
                    Dim c As Char = DBDiffPath(j)
                    If c = "\" Then
                        index = j
                    End If
                Next

            Catch ex As Exception

            End Try
            Try
                TOOO = (Convert.ToInt32(DBDiffPath.Length) - 4) - (index + 1)
                Dim Diffname As String = DBDiffPath.Substring(index + 1, TOOO).Replace("dif", ".dif")
                If File.Exists(DBDiffPath) Then
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " شروع اگسترکت کردن فایل زیپ دیفرنشیال بک آپ ", _Tmpdb)

                    ZipFile.ExtractToDirectory(DBDiffPath, FolderPath + "ForFtpTest")

                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " پایان اگسترکت کردن فایل زیپ دیفرنشیال یک آپ ", _Tmpdb)

                End If
                DBDiffPath = FolderPath + "ForFtpTest\" + Diffname

            Catch ex As Exception

            End Try

            If dt.Rows.Count = 0 Then
                com = "USE [master] 
                RESTORE DATABASE [" & DBName & "_TestForFTP] FROM  DISK = N'" & DBFullPath & "' WITH  FILE = 1,  MOVE N'" & MdfName & "' TO N'" & FolderPath & "ForFtpTest\" & DBName & "_TestForFTP.mdf',  MOVE N'" & LdfName & "' TO N'" & FolderPath & "ForFtpTest\" & DBName & "_Test.ldf',  NORECOVERY,  NOUNLOAD,  STATS = 5"
            Else
                com = "USE [master] 
                RESTORE DATABASE [" & DBName & "_TestForFTP] FROM  DISK = N'" & DBFullPath & "' WITH  FILE = 1,  MOVE N'" & MdfName & "' TO N'" & FolderPath & "ForFtpTest\" & DBName & "_TestForFTP.mdf',  MOVE N'" & LdfName & "' TO N'" & FolderPath & "ForFtpTest\" & DBName & "_Test.ldf',  NORECOVERY,  NOUNLOAD,  STATS = 5
                RESTORE DATABASE [" & DBName & "_TestForFTP] FROM  DISK = N'" & DBDiffPath & "' WITH  FILE = 1,  NOUNLOAD,  STATS = 5"
            End If

            Dim ErrorMessage As String = ""
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " شروع ریستور کردن بک آپ ", _Tmpdb)

            If ExecSc(com, ErrorMessage, _Tmpdb) Then
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " بک آپ ریستور شد ", _Tmpdb)

                Dim LastPationDT As New DataTable("DT")
                com = "USE [" & DBName & "_TestForFTP] select top 1 PRK_AdmitPatient from TBL_AdmitPatient order by PRK_AdmitPatient desc"
                LastPationDT = ExecDT(com, _Tmpdb)

                If Convert.ToInt64(LastPationDT.Rows(0)(0)) = LastAdmitID Then
                    MyMessage = "اطلاعات درست میباشد، و آماده ارسال"
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " اطلاعات بک آپ فول و دیفرنشیال گرفته شده درست میباشد ", _Tmpdb)

                    DropDB(DBName, "", _Tmpdb)

                    DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)

                    Return True
                Else
                    SendToFTPOrder = SendToFTPOrder + 1
                    SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " آخرین پذیرش های بک آپ ریستور شده و شماره ذخیره شده در لاگ ها با هم برابر نمیباشند ", _Tmpdb)
                    MyMessage = "بانک به درستی بازیابی شد ولی آخرین پذیرش درست نمیباشد"
                    DropDB(DBName, "", _Tmpdb)
                    DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)

                    Return False
                End If
            Else
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " بک آپ به درستی ریستارت نشد ", _Tmpdb)
                MyMessage = "در بازیابی بانک اطلاعاتی مشکلی بوجود آمده است"
                DropDB(DBName, "", _Tmpdb)
                DeleteFileInDirectory(FolderPath + "ForFtpTest", _Tmpdb)

                Return False
            End If
        Catch ex As Exception
            SendToFTPOrder = SendToFTPOrder + 1
            SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " Error :  " + ex.Message.ToString(), _Tmpdb)
            SaveTextExeption("Error : " + ex.Message.ToString())
            Return False

        End Try
        Return False

    End Function

    Public Function ExecDT(Command As String, _Tmpdb As Parsic.DataAccess.DBase) As DataTable
        'Dim con As SqlConnection
        Dim dt As New DataTable("DT")
        Dim connection As String = "server=192.168.1.20\SQL2017;database=DB_Afra9701_9901;user id=sa;password=****************"
        Try

            _Tmpdb.dt_filler(dt, Command, CommandType.Text)

            'con = New SqlConnection(connection)
            'con.Open()
            'Dim cmd As SqlCommand = New SqlCommand(Command, con)
            'Dim ad As SqlDataAdapter = New SqlDataAdapter(cmd)
            'cmd.ExecuteNonQuery()
            'ad.Fill(dt)
            'con.Close()
        Catch ex As Exception
            'con.Close()
        End Try
        Return dt
    End Function

    Public Function ExecSc(Command As String, ByRef ErrorMessage As String, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        'Dim con As SqlConnection

        Dim connection As String = "server=192.168.1.20\SQL2017;database=DB_Afra9701_9901;user id=sa;password=*********************************"

        Try

            _Tmpdb.EXECmd(Command, DataAccess.DBase.ExecuteMode._NONQEURY, 3600)

            'con = New SqlConnection(connection)
            'con.Open()
            'Dim cmd As SqlCommand = New SqlCommand(Command, con)
            'cmd.CommandTimeout = 3600
            'cmd.ExecuteNonQuery()
            'con.Close()
        Catch ex As Exception
            'con.Close()
            If ex.Message.ToString().Contains("the database does not exist") Then
                Return True
            End If
            ErrorMessage = ex.Message.ToString()
            Return False
        End Try
        Return True
    End Function

    Public Function DropDB(ByVal DbName As String, ByRef MyMessage As String, _Tmpdb As Parsic.DataAccess.DBase) As Boolean
        Dim cmd As String = "EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'" & DbName & "_TestForFTP' USE [master]  ALTER DATABASE [" & DbName & "_TestForFTP] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE  USE [master]  DROP DATABASE [" & DbName & "_TestForFTP]  USE [" & DbName & "]"
        For i As Int16 = 0 To 4
            If ExecSc(cmd, MyMessage, _Tmpdb) Then
                Exit For
            End If
        Next
        Return True
    End Function

    Public Function DeleteFileInDirectory(Path As String, _Tmpdb As Parsic.DataAccess.DBase)
        'Dim strFileSize As String = ""
        Dim di As New IO.DirectoryInfo(Path)
        Dim aryFi As IO.FileInfo() = di.GetFiles("*.*")
        Dim fi As IO.FileInfo

        For Each fi In aryFi
            'strFileSize = (Math.Round(fi.Length / 1024)).ToString()
            'Console.WriteLine("File Name: {0}", fi.Name)
            'Console.WriteLine("File Full Name: {0}", fi.FullName)
            'Console.WriteLine("File Size (KB): {0}", strFileSize)
            'Console.WriteLine("File Extension: {0}", fi.Extension)
            'Console.WriteLine("Last Accessed: {0}", fi.LastAccessTime)
            'Console.WriteLine("Read Only: {0}", (fi.Attributes.ReadOnly = True).ToString)
            Try
                File.Delete(fi.FullName)
            Catch ex As Exception
                SendToFTPOrder = SendToFTPOrder + 1
                SaveSubErrorLogInDB(0, 0, 0, "", "", "", "", "", SendToFTPOrder, 1000, " خطا در حذف فایل " + fi.FullName, _Tmpdb)
                SaveTextExeption("Error In delete Folder : " + Path)
            End Try
        Next

    End Function




#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═════════════════ cleareBackups ══════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "cleare Backups"


    Public Sub CleareBackups(_Tmpdb As Parsic.DataAccess.DBase)
        SaveBackupLogs(0, " شروع ترد پاک کننده بک آپ ها")

        Dim Optimize As Boolean = True

        If Optimize Then

            While StopRecivingProcess = False
                Try
                    Dim NoDelete As New List(Of Int32)

                    Dim dt As New DataTable("DT")

                    Dim today As Date = MyCommon.MyDbutility.ShamsiDate(Date.Now)

                    Dim year As String = today.Year.ToString()

                    Dim UntilDate As String = Date.Now().AddDays(-1).ToShortDateString
                    Try
                        _Tmpdb.dt_filler(dt, "select * from Tbl_Full_Diff_BackupLogs where Int_Status = 1 and cast(Str_BackupSize as bigint) > cast(1 as bigint) and  cast(Str_StartDate as date) > cast('" + year + "/01/01' as date) and cast(Str_StartDate as date) < cast('" + UntilDate + "' as date) order by Prk_FullDiffLabsBackupLogs_AutoID desc ", CommandType.Text)

                        'dv = New DataView(dt, "Str_BackupType = 'Full' and cast(Str_StartDate as date) > cast('2020/01/01' as date) and cast(Str_StartDate as date) < cast('2020/02/01' as date) ", "Prk_FullDiffLabsBackupLogs_AutoID", DataViewRowState.CurrentRows)
                    Catch ex As Exception
                        SaveTextExeption("Error In Clear (Fetch Logs Info): " + ex.Message.ToString())
                    End Try




                    If dt.Rows.Count = 0 Then
                        SaveBackupLogs(0, "بک آپی تا قبل از دو روز پیش پیدا نشد")
                    Else

                        Try
                            Dim Fullcounter As Int32 = 0
                            For i As Int32 = 0 To dt.Rows.Count() - 1
                                If dt.Rows(i)("Str_BackupType").ToString() = "Full" Then
                                    Dim path As String = dt.Rows(i)("Str_DBBackupPath").ToString()
                                    If File.Exists(path) Then
                                        NoDelete.Add(dt.Rows(i)("Prk_FullDiffLabsBackupLogs_AutoID"))
                                        Fullcounter += 1
                                    End If
                                    If Fullcounter >= 2 Then
                                        Exit For
                                    End If
                                End If
                            Next
                        Catch ex As Exception
                            SaveTextExeption("Error In Clear (Find Full Path): " + ex.Message.ToString())
                        End Try

                        Try
                            Dim Diffcounter As Int32 = 0
                            For i As Int32 = 0 To dt.Rows.Count() - 1
                                If dt.Rows(i)("Str_BackupType").ToString() = "Diff" Then
                                    Dim path As String = dt.Rows(i)("Str_DBBackupPath").ToString()
                                    If File.Exists(path) Then
                                        NoDelete.Add(dt.Rows(i)("Prk_FullDiffLabsBackupLogs_AutoID"))
                                        Diffcounter += 1
                                    End If
                                    If Diffcounter >= 3 Then
                                        Exit For
                                    End If
                                End If
                            Next
                        Catch ex As Exception

                        End Try

                        Try
                            For i As Int32 = 0 To dt.Rows.Count() - 1
                                If NoDelete.Contains(dt.Rows(i)("Prk_FullDiffLabsBackupLogs_AutoID")) Then
                                Else
                                    Dim path As String = dt.Rows(i).Item("Str_DBBackupPath").ToString()
                                    Dim ID As String = dt.Rows(i).Item("Prk_FullDiffLabsBackupLogs_AutoID").ToString()
                                    Try
                                        File.Delete(path)
                                        _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("update Tbl_Full_Diff_BackupLogs set Int_Status = -1 where Prk_FullDiffLabsBackupLogs_AutoID = " + ID))
                                    Catch ex As Exception
                                        If ex.Message.ToString().Contains("The network path was not found.") Then
                                            _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("update Tbl_Full_Diff_BackupLogs set Int_Status = -1 where Prk_FullDiffLabsBackupLogs_AutoID = " + ID))
                                        Else
                                            SaveTextExeption("Error In Clear (Delete File Path) : " + ex.Message.ToString())
                                        End If

                                    End Try
                                End If
                            Next
                        Catch ex As Exception
                            SaveTextExeption("Error In Clear (Delete Files) : " + ex.Message.ToString())
                        End Try

                    End If

                    Thread.Sleep(12 * 60 * 60 * 1000)
                Catch ex As Exception
                    SaveTextExeption("Error In Clear and run in 1 hour later Error is : " + ex.Message.ToString())
                    Thread.Sleep(60 * 60 * 1000)
                End Try

            End While

        Else

            While StopRecivingProcess = False
                Try


                    Dim NoDelete As New List(Of Int32)

                    Dim dt As New DataTable("DT")

                    Dim today As Date = MyCommon.MyDbutility.ShamsiDate(Date.Now)

                    Dim year As String = today.Year.ToString()

                    Dim UntilDate As String = Date.Now().AddDays(-6).ToShortDateString

                    _Tmpdb.dt_filler(dt, "select * from Tbl_Full_Diff_BackupLogs where Int_Status = 1 and Str_BackupSize > cast(0 as bigint) and  cast(Str_StartDate as date) > cast('" + year + "/01/01' as date) and cast(Str_StartDate as date) < cast('" + UntilDate + "' as date) ", CommandType.Text)

                    'dv = New DataView(dt, "Str_BackupType = 'Full' and cast(Str_StartDate as date) > cast('2020/01/01' as date) and cast(Str_StartDate as date) < cast('2020/02/01' as date) ", "Prk_FullDiffLabsBackupLogs_AutoID", DataViewRowState.CurrentRows)
                    If dt.Rows.Count = 0 Then
                        SaveBackupLogs(0, "بک آپی تا قبل از یک هفته پیش پیدا نشد")
                    Else
                        Dim Mount As Int32 = 1

                        While Mount <= 12

                            Dim dv As New DataView
                            Dim start As String = year + "/" + Mount.ToString() + "/01"
                            Dim Finish As String = year + "/" + (Mount + 1).ToString() + "/01"



                            If Mount = 12 Then
                                Finish = year + "/" + (Mount).ToString() + "/30"
                            End If
                            start = MyCommon.MyDbutility.MiladiDate(start)
                            Finish = MyCommon.MyDbutility.MiladiDate(Finish)

                            Dim index As Int16 = 0

                            Dim ErrorCounter2 As Int16 = 0
                            Try
                                dv = (From r In dt Where r.Item("Str_BackupType") = "Full" And Convert.ToDateTime(r.Item("Str_StartDate")) >= Convert.ToDateTime(start) And Convert.ToDateTime(r.Item("Str_StartDate")) < Convert.ToDateTime(Finish) Select r).AsDataView()
                                index = dv.Count() - 1
                                If dv.Count <> 0 Then

                                    While True
                                        Dim path As String = dv.Item(index).Item("Str_DBBackupPath").ToString()
                                        If File.Exists(path) Then
                                            NoDelete.Add(dv.Item(index).Item("Prk_FullDiffLabsBackupLogs_AutoID"))
                                            Exit While
                                        End If
                                        index = index - 1
                                    End While
                                End If
                            Catch ex As Exception
                                SaveTextExeption("Error In Clear 1 : " + ex.Message.ToString())
                                ErrorCounter2 += 1
                                If ErrorCounter2 = 3 Then
                                    Exit While
                                End If
                            End Try

                            Try

                                dv = (From r In dt Where r.Item("Str_BackupType") = "Diff" And Convert.ToDateTime(r.Item("Str_StartDate")) >= Convert.ToDateTime(start) And Convert.ToDateTime(r.Item("Str_StartDate")) < Convert.ToDateTime(Finish) Select r).AsDataView()
                                index = dv.Count() - 1
                                If dv.Count <> 0 Then
                                    Dim ErrorCounter As Int16 = 0
                                    While True
                                        Try
                                            Dim path As String = dv.Item(index).Item("Str_DBBackupPath").ToString()
                                            If File.Exists(path) Then
                                                Try
                                                    If (dv.Item(index).Item("Prk_FullDiffLabsBackupLogs_AutoID") > NoDelete(NoDelete.Count - 1)) Then
                                                        NoDelete.Add(dv.Item(index).Item("Prk_FullDiffLabsBackupLogs_AutoID"))
                                                        Exit While
                                                    End If
                                                Catch ex As Exception
                                                    Exit While
                                                End Try
                                            End If
                                            index = index - 1
                                        Catch ex As Exception
                                            SaveTextExeption("Error In Clear 2 : " + ex.Message.ToString())
                                            ErrorCounter += 1
                                            If ErrorCounter = 3 Then
                                                Exit While
                                            End If
                                        End Try

                                    End While

                                End If

                            Catch ex As Exception
                                SaveTextExeption("Error In Clear 3 : " + ex.Message.ToString())
                            End Try
                            Mount = Mount + 1

                        End While
                        Try
                            For i As Int32 = 0 To dt.Rows.Count() - 1
                                If NoDelete.Contains(dt.Rows(i)("Prk_FullDiffLabsBackupLogs_AutoID")) Then
                                Else
                                    Dim path As String = dt.Rows(i).Item("Str_DBBackupPath").ToString()
                                    Dim ID As String = dt.Rows(i).Item("Prk_FullDiffLabsBackupLogs_AutoID").ToString()
                                    Try
                                        File.Delete(path)
                                        _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("update Tbl_Full_Diff_BackupLogs set Int_Status = -1 where Prk_FullDiffLabsBackupLogs_AutoID = " + ID))
                                    Catch ex As Exception
                                        If ex.Message.ToString().Contains("The network path was not found.") Then
                                            _Tmpdb.EXECScalar([Public].CLS_Public.CHange_ISOlationLevel("update Tbl_Full_Diff_BackupLogs set Int_Status = -1 where Prk_FullDiffLabsBackupLogs_AutoID = " + ID))
                                        Else
                                            SaveTextExeption("Error In Clear 4 : " + ex.Message.ToString())
                                        End If


                                    End Try
                                End If
                            Next
                        Catch ex As Exception
                            SaveTextExeption("Error In Clear 5 : " + ex.Message.ToString())
                        End Try
                    End If
                    Thread.Sleep(6 * 60 * 60 * 1000)
                Catch ex As Exception
                    SaveTextExeption("Error In Clear 6 and run in 60 second later Error is : " + ex.Message.ToString())
                    Thread.Sleep(60 * 1000)
                End Try

            End While

        End If



    End Sub


#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝




End Class
