Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.IO
Imports SharpAvi
Imports SharpAvi.Codecs
Imports SharpAvi.Output
Public Class aviScreenCapture
    Dim picCount As Integer
    Dim myLock As New Mutex(True, "myLock")
    Dim startTime As DateTime
    Dim duration As Integer
    Dim codec As FourCC
    Dim screenThread As Thread
    Dim threadStop As Boolean
    Dim aviFileName As String
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If IsNumeric(TextBox1.Text) = False Then TextBox1.Text = "5"
        If Button1.Text = "Start" Then
            SaveSetting("aviScreenCapture", "duration", "duration", TextBox1.Text)
            SaveSetting("aviScreenCapture", "filename", "filename", fileName.Text)

            duration = CInt(TextBox1.Text)
            startTime = Now()
            Button1.Text = "Stop"
            Timer1.Interval = 30
            Timer1.Enabled = True

            threadStop = False
            screenThread = New Thread(AddressOf RecordScreen)
            screenThread.Name = "RecordStream"
            screenThread.IsBackground = True
            screenThread.Start()
        Else
            threadStop = True
            screenThread.Join()
            Timer1.Enabled = False

            Button1.Text = "Start"
            Dim diff = Now().Subtract(startTime)
            Me.Text = "Captured " + CStr(picCount) + " frames in " + Format(diff.TotalMilliseconds / 1000, "#0.0") + " seconds.  FPS = " + Format(picCount / diff.TotalSeconds, "#0.0")
            Me.Width = Me.Width + 1
        End If
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Static lastPicCount = picCount
        If lastPicCount = picCount Then Exit Sub
        lastPicCount = picCount
        If Label3.Height >= Me.Height - Label3.Top - 80 Then Label3.Text = ""
        Label3.Text += vbCrLf + "Capturing frame " + CStr(picCount)
        If Now().Subtract(startTime).Seconds > duration Then Button1_Click(sender, e)
    End Sub
    Private Sub appCapture_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        codec = KnownFourCCs.Codecs.MotionJpeg
        Dim fileinfo As New FileInfo(Application.StartupPath + "/../../Video/sample.avi")
        aviFileName = fileinfo.FullName
        TextBox1.Text = GetSetting("aviScreenCapture", "duration", "duration", "1")
        fileName.Text = GetSetting("aviScreenCapture", "filename", "filename", aviFileName)
    End Sub
    Private Sub RecordScreen()
        Dim offset = 8
        Dim top = Me.Top
        Dim left = Me.Left + offset
        Dim w = Width - 2 * offset
        Dim h = Height - offset

        Dim writer As AviWriter
        Dim videoStream As IAviVideoStream
        writer = New AviWriter(fileName.Text)
        videoStream = writer.AddMotionJpegVideoStream(w, h, 70)
        Dim buffer(w * h * 4) As Byte
        Dim bmp = New Bitmap(w, h)
        Dim videoWriteTask As Task = Nothing
        While 1
            Console.WriteLine("frame = " + CStr(picCount) + " w = " + CStr(w) + " h = " + CStr(h))
            Application.DoEvents()

            Dim g = Graphics.FromImage(bmp)
            g.CompositingQuality = Drawing2D.CompositingQuality.HighQuality
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
            g.CopyFromScreen(New Point(left, top), New Point(0, 0), New Drawing.Size(w, h))
            g.Flush()
            Dim bits = bmp.LockBits(New Rectangle(0, 0, w, h), Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppPArgb)
            Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length)
            bmp.UnlockBits(bits)

            videoWriteTask?.Wait()
            videoWriteTask = videoStream.WriteFrameAsync(True, buffer, 0, buffer.Length)
            picCount += 1
            If threadStop = True Then Exit While
        End While
        videoWriteTask?.Wait()
        writer.Close()
    End Sub
End Class
