<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.Btn_Start = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Txt_DBName = New System.Windows.Forms.TextBox()
        Me.Txt_Connection = New System.Windows.Forms.TextBox()
        Me.Txt_Path = New System.Windows.Forms.TextBox()
        Me.Txt_Hour = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Txt_DiffHure = New System.Windows.Forms.TextBox()
        Me.Btn_SendEmail = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.Txt_M = New System.Windows.Forms.TextBox()
        Me.Txt_H = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Dg_LastBackups = New System.Windows.Forms.DataGridView()
        Me.Btn_Logs = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Lable1 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Txt_RootPath = New System.Windows.Forms.TextBox()
        Me.Txt_FtpLoclPath = New System.Windows.Forms.TextBox()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Txt_BackupName = New System.Windows.Forms.TextBox()
        Me.Txt_SplitName = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Txt_Answer = New System.Windows.Forms.TextBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Column1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column3 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column4 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column5 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column6 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column7 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column8 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.GroupBox1.SuspendLayout()
        CType(Me.Dg_LastBackups, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Btn_Start
        '
        Me.Btn_Start.Location = New System.Drawing.Point(316, 12)
        Me.Btn_Start.Name = "Btn_Start"
        Me.Btn_Start.Size = New System.Drawing.Size(269, 29)
        Me.Btn_Start.TabIndex = 0
        Me.Btn_Start.Text = "Start"
        Me.Btn_Start.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(55, 21)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(54, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "DBName :"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(14, 71)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(99, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Connection String :"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(76, 48)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(36, 13)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "Path :"
        '
        'Txt_DBName
        '
        Me.Txt_DBName.Location = New System.Drawing.Point(117, 18)
        Me.Txt_DBName.Name = "Txt_DBName"
        Me.Txt_DBName.Size = New System.Drawing.Size(193, 21)
        Me.Txt_DBName.TabIndex = 4
        Me.Txt_DBName.Text = "parsipol_developing"
        '
        'Txt_Connection
        '
        Me.Txt_Connection.Location = New System.Drawing.Point(117, 70)
        Me.Txt_Connection.Name = "Txt_Connection"
        Me.Txt_Connection.Size = New System.Drawing.Size(682, 21)
        Me.Txt_Connection.TabIndex = 5
        Me.Txt_Connection.Text = "server=PWS\PWS;database=parsipol_developing;user id=pwsservices;password=123qwe!@" &
    "#asd#@!ZXC$%^ASD;Max Pool Size=800;"
        '
        'Txt_Path
        '
        Me.Txt_Path.Location = New System.Drawing.Point(117, 44)
        Me.Txt_Path.Name = "Txt_Path"
        Me.Txt_Path.Size = New System.Drawing.Size(468, 21)
        Me.Txt_Path.TabIndex = 6
        Me.Txt_Path.Text = "C:\BACKUP_For_Parsic_FTP\"
        '
        'Txt_Hour
        '
        Me.Txt_Hour.Location = New System.Drawing.Point(1008, 497)
        Me.Txt_Hour.Name = "Txt_Hour"
        Me.Txt_Hour.Size = New System.Drawing.Size(65, 21)
        Me.Txt_Hour.TabIndex = 7
        Me.Txt_Hour.Text = "24"
        Me.Txt_Hour.Visible = False
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(923, 500)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(77, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Backup Hour : "
        Me.Label4.Visible = False
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(1137, 500)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(69, 13)
        Me.Label5.TabIndex = 10
        Me.Label5.Text = "Diff minute : "
        Me.Label5.Visible = False
        '
        'Txt_DiffHure
        '
        Me.Txt_DiffHure.Location = New System.Drawing.Point(1209, 497)
        Me.Txt_DiffHure.Name = "Txt_DiffHure"
        Me.Txt_DiffHure.Size = New System.Drawing.Size(65, 21)
        Me.Txt_DiffHure.TabIndex = 9
        Me.Txt_DiffHure.Text = "15"
        Me.Txt_DiffHure.Visible = False
        '
        'Btn_SendEmail
        '
        Me.Btn_SendEmail.Location = New System.Drawing.Point(182, 32)
        Me.Btn_SendEmail.Name = "Btn_SendEmail"
        Me.Btn_SendEmail.Size = New System.Drawing.Size(95, 43)
        Me.Btn_SendEmail.TabIndex = 11
        Me.Btn_SendEmail.Text = "لاگ ابری ایمیل"
        Me.Btn_SendEmail.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.Txt_M)
        Me.GroupBox1.Controls.Add(Me.Btn_SendEmail)
        Me.GroupBox1.Controls.Add(Me.Txt_H)
        Me.GroupBox1.Controls.Add(Me.Label7)
        Me.GroupBox1.Controls.Add(Me.Label6)
        Me.GroupBox1.Location = New System.Drawing.Point(918, 381)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(310, 100)
        Me.GroupBox1.TabIndex = 12
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "GroupBox1"
        Me.GroupBox1.Visible = False
        '
        'Txt_M
        '
        Me.Txt_M.Location = New System.Drawing.Point(23, 70)
        Me.Txt_M.Name = "Txt_M"
        Me.Txt_M.Size = New System.Drawing.Size(54, 21)
        Me.Txt_M.TabIndex = 3
        Me.Txt_M.Text = "59"
        '
        'Txt_H
        '
        Me.Txt_H.Location = New System.Drawing.Point(23, 32)
        Me.Txt_H.Name = "Txt_H"
        Me.Txt_H.Size = New System.Drawing.Size(54, 21)
        Me.Txt_H.TabIndex = 2
        Me.Txt_H.Text = "23"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(98, 73)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(33, 13)
        Me.Label7.TabIndex = 1
        Me.Label7.Text = "دقیقه"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(98, 35)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(38, 13)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "ساعت"
        '
        'Dg_LastBackups
        '
        Me.Dg_LastBackups.AllowUserToAddRows = False
        Me.Dg_LastBackups.AllowUserToDeleteRows = False
        Me.Dg_LastBackups.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Dg_LastBackups.BackgroundColor = System.Drawing.SystemColors.ButtonFace
        Me.Dg_LastBackups.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Dg_LastBackups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Dg_LastBackups.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Column1, Me.Column2, Me.Column3, Me.Column4, Me.Column5, Me.Column6, Me.Column7, Me.Column8})
        Me.Dg_LastBackups.Location = New System.Drawing.Point(5, 97)
        Me.Dg_LastBackups.Name = "Dg_LastBackups"
        Me.Dg_LastBackups.ReadOnly = True
        Me.Dg_LastBackups.RowHeadersVisible = False
        Me.Dg_LastBackups.RowHeadersWidth = 50
        Me.Dg_LastBackups.Size = New System.Drawing.Size(793, 383)
        Me.Dg_LastBackups.TabIndex = 116
        '
        'Btn_Logs
        '
        Me.Btn_Logs.Location = New System.Drawing.Point(591, 11)
        Me.Btn_Logs.Name = "Btn_Logs"
        Me.Btn_Logs.Size = New System.Drawing.Size(207, 54)
        Me.Btn_Logs.TabIndex = 117
        Me.Btn_Logs.Text = "Logs"
        Me.Btn_Logs.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(669, 370)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 118
        Me.Button1.Text = "folder"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(48, 372)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(556, 21)
        Me.TextBox1.TabIndex = 119
        '
        'TextBox2
        '
        Me.TextBox2.Location = New System.Drawing.Point(48, 399)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(556, 21)
        Me.TextBox2.TabIndex = 120
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(669, 399)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 121
        Me.Button2.Text = "file"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Lable1
        '
        Me.Lable1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Lable1.AutoSize = True
        Me.Lable1.Location = New System.Drawing.Point(8, 523)
        Me.Lable1.Name = "Lable1"
        Me.Lable1.Size = New System.Drawing.Size(65, 13)
        Me.Lable1.TabIndex = 122
        Me.Lable1.Text = "Root Path : "
        '
        'Label9
        '
        Me.Label9.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(337, 523)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(79, 13)
        Me.Label9.TabIndex = 123
        Me.Label9.Text = "Ftp Locl Path : "
        '
        'Txt_RootPath
        '
        Me.Txt_RootPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Txt_RootPath.Location = New System.Drawing.Point(95, 520)
        Me.Txt_RootPath.Name = "Txt_RootPath"
        Me.Txt_RootPath.Size = New System.Drawing.Size(236, 21)
        Me.Txt_RootPath.TabIndex = 124
        '
        'Txt_FtpLoclPath
        '
        Me.Txt_FtpLoclPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Txt_FtpLoclPath.Location = New System.Drawing.Point(422, 520)
        Me.Txt_FtpLoclPath.Name = "Txt_FtpLoclPath"
        Me.Txt_FtpLoclPath.Size = New System.Drawing.Size(286, 21)
        Me.Txt_FtpLoclPath.TabIndex = 125
        '
        'Button3
        '
        Me.Button3.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button3.Location = New System.Drawing.Point(723, 520)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(75, 46)
        Me.Button3.TabIndex = 126
        Me.Button3.Text = "Assemble"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Label10
        '
        Me.Label10.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(8, 548)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(81, 13)
        Me.Label10.TabIndex = 122
        Me.Label10.Text = "Backup Name : "
        '
        'Label11
        '
        Me.Label11.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(337, 548)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(67, 13)
        Me.Label11.TabIndex = 123
        Me.Label11.Text = "Split Name : "
        '
        'Txt_BackupName
        '
        Me.Txt_BackupName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Txt_BackupName.Location = New System.Drawing.Point(95, 545)
        Me.Txt_BackupName.Name = "Txt_BackupName"
        Me.Txt_BackupName.Size = New System.Drawing.Size(236, 21)
        Me.Txt_BackupName.TabIndex = 124
        '
        'Txt_SplitName
        '
        Me.Txt_SplitName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Txt_SplitName.Location = New System.Drawing.Point(422, 545)
        Me.Txt_SplitName.Name = "Txt_SplitName"
        Me.Txt_SplitName.Size = New System.Drawing.Size(286, 21)
        Me.Txt_SplitName.TabIndex = 125
        '
        'Label8
        '
        Me.Label8.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(8, 575)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(53, 13)
        Me.Label8.TabIndex = 127
        Me.Label8.Text = "Answer : "
        '
        'Txt_Answer
        '
        Me.Txt_Answer.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Txt_Answer.Location = New System.Drawing.Point(95, 572)
        Me.Txt_Answer.Multiline = True
        Me.Txt_Answer.Name = "Txt_Answer"
        Me.Txt_Answer.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.Txt_Answer.Size = New System.Drawing.Size(703, 62)
        Me.Txt_Answer.TabIndex = 128
        '
        'Label12
        '
        Me.Label12.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(249, 483)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(549, 13)
        Me.Label12.TabIndex = 129
        Me.Label12.Text = "در صورتی که فایل ها انتقال پیدا کردند ولی اسمبل نشدند از این قسمت برای اسمبل کردن" &
    " فایل ها میتونید استفاده کنید"
        '
        'Label13
        '
        Me.Label13.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(8, 492)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(218, 13)
        Me.Label13.TabIndex = 130
        Me.Label13.Text = "C:\ParsicWebTemp\AutoBackupErrorLog.txt"
        '
        'Label14
        '
        Me.Label14.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(359, 501)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(439, 13)
        Me.Label14.TabIndex = 131
        Me.Label14.Text = "Txt_Assembler Info  را در فایل رو برو برای بک آپ مورد نظر سرج و در کادر های زیر ج" &
    "ایگزین کنید"
        '
        'Column1
        '
        Me.Column1.DataPropertyName = "نوع"
        Me.Column1.HeaderText = "نوع"
        Me.Column1.Name = "Column1"
        Me.Column1.ReadOnly = True
        Me.Column1.Width = 50
        '
        'Column2
        '
        Me.Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.Column2.DataPropertyName = "پیام"
        Me.Column2.HeaderText = "پیام"
        Me.Column2.Name = "Column2"
        Me.Column2.ReadOnly = True
        '
        'Column3
        '
        Me.Column3.DataPropertyName = "تاریخ"
        Me.Column3.HeaderText = "تاریخ"
        Me.Column3.Name = "Column3"
        Me.Column3.ReadOnly = True
        Me.Column3.Width = 70
        '
        'Column4
        '
        Me.Column4.DataPropertyName = "ساعت"
        Me.Column4.HeaderText = "ساعت"
        Me.Column4.Name = "Column4"
        Me.Column4.ReadOnly = True
        Me.Column4.Width = 70
        '
        'Column5
        '
        Me.Column5.DataPropertyName = "آدرس"
        Me.Column5.HeaderText = "آدرس"
        Me.Column5.Name = "Column5"
        Me.Column5.ReadOnly = True
        '
        'Column6
        '
        Me.Column6.DataPropertyName = "ضعیت"
        Me.Column6.HeaderText = "وضعیت"
        Me.Column6.Name = "Column6"
        Me.Column6.ReadOnly = True
        Me.Column6.Width = 50
        '
        'Column7
        '
        Me.Column7.DataPropertyName = "ارسال به شرکت"
        Me.Column7.HeaderText = "ارسال"
        Me.Column7.Name = "Column7"
        Me.Column7.ReadOnly = True
        Me.Column7.Width = 50
        '
        'Column8
        '
        Me.Column8.DataPropertyName = "حذف شده"
        Me.Column8.HeaderText = "حذف"
        Me.Column8.Name = "Column8"
        Me.Column8.ReadOnly = True
        Me.Column8.Width = 50
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(810, 640)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Txt_Answer)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Txt_SplitName)
        Me.Controls.Add(Me.Txt_FtpLoclPath)
        Me.Controls.Add(Me.Txt_BackupName)
        Me.Controls.Add(Me.Txt_RootPath)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.Lable1)
        Me.Controls.Add(Me.Btn_Logs)
        Me.Controls.Add(Me.Dg_LastBackups)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Txt_DiffHure)
        Me.Controls.Add(Me.Btn_Start)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Txt_Hour)
        Me.Controls.Add(Me.Txt_Path)
        Me.Controls.Add(Me.Txt_Connection)
        Me.Controls.Add(Me.Txt_DBName)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.TextBox2)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Button1)
        Me.Font = New System.Drawing.Font("Tahoma", 8.25!)
        Me.MinimumSize = New System.Drawing.Size(826, 616)
        Me.Name = "Form1"
        Me.Text = "PWS Backuper On FTP"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.Dg_LastBackups, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Btn_Start As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Txt_DBName As TextBox
    Friend WithEvents Txt_Connection As TextBox
    Friend WithEvents Txt_Path As TextBox
    Friend WithEvents Txt_Hour As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Txt_DiffHure As TextBox
    Friend WithEvents Btn_SendEmail As Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Txt_M As TextBox
    Friend WithEvents Txt_H As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Dg_LastBackups As DataGridView
    Friend WithEvents Btn_Logs As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents Button2 As Button
    Friend WithEvents Lable1 As Label
    Friend WithEvents Label9 As Label
    Friend WithEvents Txt_RootPath As TextBox
    Friend WithEvents Txt_FtpLoclPath As TextBox
    Friend WithEvents Button3 As Button
    Friend WithEvents Label10 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents Txt_BackupName As TextBox
    Friend WithEvents Txt_SplitName As TextBox
    Friend WithEvents Label8 As Label
    Friend WithEvents Txt_Answer As TextBox
    Friend WithEvents Label12 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents Label14 As Label
    Friend WithEvents Column1 As DataGridViewTextBoxColumn
    Friend WithEvents Column2 As DataGridViewTextBoxColumn
    Friend WithEvents Column3 As DataGridViewTextBoxColumn
    Friend WithEvents Column4 As DataGridViewTextBoxColumn
    Friend WithEvents Column5 As DataGridViewTextBoxColumn
    Friend WithEvents Column6 As DataGridViewTextBoxColumn
    Friend WithEvents Column7 As DataGridViewTextBoxColumn
    Friend WithEvents Column8 As DataGridViewTextBoxColumn
End Class
