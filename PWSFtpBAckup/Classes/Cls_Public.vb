Imports System

Public Class Cls_Public

    '╔══════════════════ Constructor ═══════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Constructor"
    Sub New()

    End Sub
#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═══════════════════ Variabel ═════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Variabel"

    Public PersianMonth() As String = {"فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"}
    Dim MySecurity As New Parsic.Business.Security.Cls_Encryption

#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═════════════════════ Enum ═══════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Enum"
    Public Enum ENUM_Languages
        Farsi = 1
        English = 2
    End Enum

    Enum ShamsiDateOption
        YYYY_MM_DD
        YY_MM_DD
        MM_DD
        YYMMDD
        YY_MM
    End Enum
#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝

    '╔═══════════════════ Function ═════════════════════╗
    '║                                                  ║
    '║                                                  ║
#Region "Function"

    Public Sub ChangeLanguage(ByVal SelectedLanguage As ENUM_Languages)
        Try

            If SelectedLanguage = ENUM_Languages.Farsi Then
                For i As Integer = 0 To InputLanguage.InstalledInputLanguages.Count - 1
                    '   If LCase(InputLanguage.InstalledInputLanguages(I).LayoutName) = "farsi" Then
                    InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages(i)
                    '   End If
                Next
            Else
                For i As Integer = 0 To InputLanguage.InstalledInputLanguages.Count - 1
                    If LCase(InputLanguage.InstalledInputLanguages(i).LayoutName) = "us" Then
                        InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages(i)
                    End If
                Next
            End If

        Catch ex As Exception
        End Try
    End Sub

    Public Function ShamsiDate(ByVal MilDate As Object, Optional ByVal OutFormat As Cls_Public.ShamsiDateOption = ShamsiDateOption.YYYY_MM_DD) As String

        Try


            Const conFirstDay = 7749
            Dim FiveYearLeaps(6) As Integer
            FiveYearLeaps(0) = 9
            FiveYearLeaps(1) = 42
            FiveYearLeaps(2) = 75
            FiveYearLeaps(3) = 108
            FiveYearLeaps(4) = 141
            FiveYearLeaps(5) = 174
            Dim FYLeapdDays(7) As Integer
            FYLeapdDays(0) = 3287
            FYLeapdDays(1) = 15340
            FYLeapdDays(2) = 27393
            FYLeapdDays(3) = 39446
            FYLeapdDays(4) = 51499
            FYLeapdDays(5) = 63552
            FYLeapdDays(6) = 75971
            Dim InterYears(33) As Integer
            InterYears(0) = 366
            InterYears(1) = 731
            InterYears(2) = 1096
            InterYears(3) = 1461
            InterYears(4) = 1827
            InterYears(5) = 2192
            InterYears(6) = 2557
            InterYears(7) = 2922
            InterYears(8) = 3288
            InterYears(9) = 3653
            InterYears(10) = 4018
            InterYears(11) = 4383
            InterYears(12) = 4749
            InterYears(13) = 5114
            InterYears(14) = 5479
            InterYears(15) = 5844
            InterYears(16) = 6210
            InterYears(17) = 6575
            InterYears(18) = 6940
            InterYears(19) = 7305
            InterYears(20) = 7671
            InterYears(21) = 8036
            InterYears(22) = 8401
            InterYears(23) = 8766
            InterYears(24) = 9132
            InterYears(25) = 9497
            InterYears(26) = 9862
            InterYears(27) = 10227
            InterYears(28) = 10593
            InterYears(29) = 10958
            InterYears(30) = 11323
            InterYears(31) = 11688
            InterYears(32) = 12053
            Dim DaysPassedInYear(12) As Integer
            DaysPassedInYear(0) = 31
            DaysPassedInYear(1) = 62
            DaysPassedInYear(2) = 93
            DaysPassedInYear(3) = 124
            DaysPassedInYear(4) = 155
            DaysPassedInYear(5) = 186
            DaysPassedInYear(6) = 216
            DaysPassedInYear(7) = 246
            DaysPassedInYear(8) = 276
            DaysPassedInYear(9) = 306
            DaysPassedInYear(10) = 336
            DaysPassedInYear(11) = 366
            Dim i, shYear, shMonth, shDay, Days As Integer
            Days = DateDiff("d", #1/1/1900#, MilDate)
            If Days <= conFirstDay Then
                ShamsiDate = "1300/1/1"
                Exit Function
            End If

            Days = Days - conFirstDay

            If Days >= 75971 Then
                ShamsiDate = "1508/1/1"
                Exit Function
            End If

            i = 0
            While Days > FYLeapdDays(i)
                i = i + 1
            End While
            If i > 0 Then
                Days = Days - FYLeapdDays(i - 1) + 1
                shYear = FiveYearLeaps(i - 1)
            End If
            i = 0
            While Days > InterYears(i)
                i = i + 1
            End While
            shYear = shYear + i
            If i > 0 Then Days = Days - InterYears(i - 1)

            i = 0
            While Days > DaysPassedInYear(i)
                i = i + 1
            End While
            shMonth = i + 1
            If i > 0 Then Days = Days - DaysPassedInYear(i - 1)
            shDay = Days

            Dim res As String
            Select Case OutFormat
                Case Cls_Public.ShamsiDateOption.YYYY_MM_DD
                    res = CStr(shYear + 1300) & "/" & IIf(Len(shMonth.ToString) = 1, "0" & shMonth.ToString, shMonth.ToString) & "/" & IIf(Len(shDay.ToString) = 1, "0" & shDay.ToString, shDay.ToString)
                Case Cls_Public.ShamsiDateOption.YY_MM_DD
                    res = CStr(shYear) & "/" & IIf(Len(shMonth.ToString) = 1, "0" & shMonth.ToString, shMonth.ToString) & "/" & IIf(Len(shDay.ToString) = 1, "0" & shDay.ToString, shDay.ToString)
                Case Cls_Public.ShamsiDateOption.YYMMDD
                    res = CStr(shYear) & IIf(Len(shMonth.ToString) = 1, "0" & shMonth.ToString, shMonth.ToString) & IIf(Len(shDay.ToString) = 1, "0" & shDay.ToString, shDay.ToString)
                Case Cls_Public.ShamsiDateOption.MM_DD
                    res = IIf(Len(shMonth.ToString) = 1, "0" & shMonth.ToString, shMonth.ToString) & "/" & IIf(Len(shDay.ToString) = 1, "0" & shDay.ToString, shDay.ToString)
                Case Cls_Public.ShamsiDateOption.YY_MM
                    res = CStr(shYear) & "/" & IIf(Len(shMonth.ToString) = 1, "0" & shMonth.ToString, shMonth.ToString)
            End Select

            Return res.ToString

        Catch ex As Exception

        End Try
    End Function

    Public Function MiladiDate(ByVal shamsyday As String, Optional ByVal _Format As String = "")

        Try
            Const conFirstDay = 7749
            Dim FiveYearLeaps(6) As Integer
            FiveYearLeaps(0) = 9
            FiveYearLeaps(1) = 42
            FiveYearLeaps(2) = 75
            FiveYearLeaps(3) = 108
            FiveYearLeaps(4) = 141
            FiveYearLeaps(5) = 174
            Dim FYLeapdDays(7) As Integer
            FYLeapdDays(0) = 3287
            FYLeapdDays(1) = 15340
            FYLeapdDays(2) = 27393
            FYLeapdDays(3) = 39446
            FYLeapdDays(4) = 51499
            FYLeapdDays(5) = 63552
            FYLeapdDays(6) = 75971
            Dim InterYears(33) As Integer
            InterYears(0) = 366
            InterYears(1) = 731
            InterYears(2) = 1096
            InterYears(3) = 1461
            InterYears(4) = 1827
            InterYears(5) = 2192
            InterYears(6) = 2557
            InterYears(7) = 2922
            InterYears(8) = 3288
            InterYears(9) = 3653
            InterYears(10) = 4018
            InterYears(11) = 4383
            InterYears(12) = 4749
            InterYears(13) = 5114
            InterYears(14) = 5479
            InterYears(15) = 5844
            InterYears(16) = 6210
            InterYears(17) = 6575
            InterYears(18) = 6940
            InterYears(19) = 7305
            InterYears(20) = 7671
            InterYears(21) = 8036
            InterYears(22) = 8401
            InterYears(23) = 8766
            InterYears(24) = 9132
            InterYears(25) = 9497
            InterYears(26) = 9862
            InterYears(27) = 10227
            InterYears(28) = 10593
            InterYears(29) = 10958
            InterYears(30) = 11323
            InterYears(31) = 11688
            InterYears(32) = 12053
            Dim DaysPassedInYear(12) As Integer
            DaysPassedInYear(0) = 31
            DaysPassedInYear(1) = 62
            DaysPassedInYear(2) = 93
            DaysPassedInYear(3) = 124
            DaysPassedInYear(4) = 155
            DaysPassedInYear(5) = 186
            DaysPassedInYear(6) = 216
            DaysPassedInYear(7) = 246
            DaysPassedInYear(8) = 276
            DaysPassedInYear(9) = 306
            DaysPassedInYear(10) = 336
            DaysPassedInYear(11) = 366
            Dim days, tmpYears, i, year, month, day As Integer
            year = Strings.Left(shamsyday, InStr(shamsyday, "/") - 1)
            month = Mid(shamsyday, InStr(shamsyday, "/") + 1, InStrRev(shamsyday, "/") - InStr(shamsyday, "/") - 1)
            day = Strings.Right(shamsyday, Len(shamsyday) - InStrRev(shamsyday, "/"))
            If year < 100 Then
                year = year + 1300
            End If
            tmpYears = year - 1300
            i = 0
            While tmpYears >= FiveYearLeaps(i)
                i = i + 1
            End While
            If i > 0 Then
                tmpYears = tmpYears - FiveYearLeaps(i - 1)
                days = days + FYLeapdDays(i - 1)
            End If
            If tmpYears > 0 Then days = days + InterYears(tmpYears - 1)
            If month > 1 Then days = days + DaysPassedInYear(month - 2)
            days = days + (day - 1) + conFirstDay

            'days = -1 * days
            If _Format = "" Then

                'Date : January 20,2010
                'Raz Note: ToShortDateString convert date object to a normal type that is string and in mode YYYY/MM/DD

                MiladiDate = DateAdd(DateInterval.Day, days, #1/1/1900#).ToShortDateString

            Else
                Dim Obj As Object, Y As Object, M As Object, D As Object
                Obj = DateAdd(DateInterval.Day, days, #1/1/1900#)
                Y = CType(Obj, Date).Year
                M = CType(Obj, Date).Month
                'AD By Azari At : 92/12/18
                M = IIf(Len(M.ToString) = 1, "0" & M.ToString, M.ToString)
                'End
                D = CType(Obj, Date).Day
                'ADD By Azari At:92/12/18
                D = IIf(Len(D.ToString) = 1, "0" & D.ToString, D.ToString)
                'End
                MiladiDate = Y & "/" & M & "/" & D
            End If

        Catch ex As Exception
        End Try

        '        MiladiDate = CDate(days)

    End Function

    Public Function MiladiDateNew(ByVal shamsyday As String, Optional ByVal _Format As String = "")

        Try
            Dim a As New PersianToolS.PersinToolsClass
            Dim b As Date = a.PersianToDate(shamsyday)


            Return b.Year & "/" & IIf(Len(b.Month.ToString) = 1, "0" & b.Month.ToString, b.Month.ToString) & "/" & IIf(Len(b.Day.ToString) = 1, "0" & b.Day.ToString, b.Day.ToString)

        Catch ex As Exception
            Return ""
        End Try

    End Function

    Public Function ShamsiDateNew(ByVal MilDate As Object, Optional ByVal OutFormat As Cls_Public.ShamsiDateOption = Cls_Public.ShamsiDateOption.YYYY_MM_DD) As String

        Try

            Dim a As New PersianToolS.PersinToolsClass
            Dim b As PersianToolS.PersinToolsClass.PersianDate = a.DateToPersian(CType(MilDate, DateTime))


            Dim res As String
            Select Case OutFormat

                Case Cls_Public.ShamsiDateOption.YYYY_MM_DD
                    res = b.year & "/" & IIf(Len(b.month.ToString) = 1, "0" & b.month.ToString, b.month.ToString) & "/" & IIf(Len(b.day.ToString) = 1, "0" & b.day.ToString, b.day.ToString)
                Case Cls_Public.ShamsiDateOption.YY_MM_DD
                    res = Right(b.year, 2) & "/" & IIf(Len(b.month.ToString) = 1, "0" & b.month.ToString, b.month.ToString) & "/" & IIf(Len(b.day.ToString) = 1, "0" & b.day.ToString, b.day.ToString)
                Case Cls_Public.ShamsiDateOption.YYMMDD
                    res = Right(b.year, 2) & IIf(Len(b.month.ToString) = 1, "0" & b.month.ToString, b.month.ToString) & IIf(Len(b.day.ToString) = 1, "0" & b.day.ToString, b.day.ToString)
                Case Cls_Public.ShamsiDateOption.MM_DD
                    res = IIf(Len(b.month.ToString) = 1, "0" & b.month.ToString, b.month.ToString) & "/" & IIf(Len(b.day.ToString) = 1, "0" & b.day.ToString, b.day.ToString)
                Case Cls_Public.ShamsiDateOption.YY_MM
                    res = Left(b.year, 2) & "/" & IIf(Len(b.month.ToString) = 1, "0" & b.month.ToString, b.month.ToString)

            End Select

            Return res.ToString


        Catch ex As Exception
        End Try

    End Function

    Public Function Get_UserName() As String
        ' Return "hbb" & Now.Year & "parsic" & Now.Month
        Return "Ticketing"
    End Function

    Public Function Get_Password() As String

        Try
            Dim MyKey As String = "******************************************************************************************************************"
            Dim MyTime As DateTime = Now
            Dim MyDate As String = "*****" + "*****"
            Dim MyPass As String = "*****" + "*****"

            MyPass = MySecurity.EncryptData(MyPass, MyKey)

            Return MyPass
        Catch ex As Exception
            Return ""
        End Try

    End Function

    Public Sub DGVGeneralAppearance(ByVal DgvTemp As DataGridView)

        Try

            For i As Integer = 0 To DgvTemp.RowCount

                If i Mod 2 = 0 Then
                    DgvTemp.Rows(i).DefaultCellStyle.BackColor = Color.LightCyan
                Else
                    DgvTemp.Rows(i).DefaultCellStyle.BackColor = Color.LightGray
                End If

            Next

        Catch ex As Exception
        End Try

    End Sub

    Public Function Convert_Date_To_AlphabetDate(ByVal Str_Date As String, Optional ByVal giveShort As Boolean = False) As String

        Dim Year As String = ""
        Dim Month As String = ""
        Dim Day As String = ""
        Dim DayName As String = ""
        Dim MonthName As String = ""

        Dim arrWeekDays() As String = {"شنبه", "یکشنبه", "دوشنبه", "سه شنبه", "چهار شنبه", "پنجشنبه", "جمعه"}
        Dim arrShWeekDays() As String = {"شنبه", "ی.ش", "دو.ش", "سه.ش", "چ.ش", "پ.ش", "جمعه"}
        Dim arrMonth() As String = {"فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"}
        Dim arrShMonth() As String = {"فرو.", "ارد.", "خرد.", "تیر", "مرد.", "شهر.", "مهر", "آبان", "آذر", "دی", "بهمن", "اسف."}


        Year = Mid(Str_Date, 1, InStr(Str_Date, "/") - 1)
        Month = Mid(Str_Date, InStr(Str_Date, "/") + 1, InStrRev(Str_Date, "/") - InStr(Str_Date, "/") - 1)
        Day = Mid(Str_Date, InStrRev(Str_Date, "/") + 1)

        DayName = IIf(giveShort = False, arrWeekDays(DateAndTime.Weekday(MiladiDate(Str_Date), FirstDayOfWeek.Saturday) - 1), arrShWeekDays(DateAndTime.Weekday(MiladiDate(Str_Date), FirstDayOfWeek.Saturday) - 1))

        MonthName = IIf(giveShort = False, arrMonth(Int(Month) - 1), arrShMonth(Int(Month) - 1))

        If giveShort = True Then
            If Year.Length = 4 Then
                Year = Year.Remove(0, 2)
            End If
            Return (DayName & " " & Day & " " & MonthName & "  " & Year).Trim
        End If
        If Year.Length = 4 Then
            Year = Year.Remove(0, 2)
        End If
        Return (DayName & " " & Day & " " & MonthName & "  " & Year).Trim

    End Function

    Public Shared Function GetTimeStamp() As String

        Try
            Return Now.Year.ToString & "" & IIf(Now.Month.ToString.Length = 1, "0" + Now.Month.ToString, Now.Month.ToString) & "" & IIf(Now.Day.ToString.Length = 1, "0" + Now.Day.ToString, Now.Day.ToString) & "_" & Now.Hour & Now.Minute & Now.Minute & Now.Millisecond
        Catch ex As Exception
        End Try

    End Function

    Public Function InsertSeperator(ByVal str As String) As String

        Try

            Dim DigitTemp As String = str
            Dim DigitTempDes As String = ""

            Do While DigitTemp.Length > 3

                DigitTempDes = "," + DigitTemp.Substring(DigitTemp.Length - 3, 3) + DigitTempDes

                DigitTemp = DigitTemp.Remove(DigitTemp.Length - 3)

            Loop

            DigitTempDes = DigitTemp + DigitTempDes

            If DigitTempDes.Substring(0, 1) = "," Then
                DigitTempDes = DigitTempDes.Remove(0, 1)
            End If


            Return DigitTempDes


        Catch ex As Exception

        End Try

    End Function
#End Region
    '║                                                  ║
    '║                                                  ║
    '╚══════════════════════════════════════════════════╝






End Class
