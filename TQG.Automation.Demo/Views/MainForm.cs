using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TQG.Automation.Demo.Data;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Exceptions;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.Demo.Views;
public partial class MainForm : DevExpress.XtraEditors.XtraForm
{
    private readonly GatewayBackgroundService _gatewayService;
    private readonly MovingTaskRepository _repository;
    private TransportTask? _selectedTask;
    private const string DeviceId = "Shuttle01";

    public MainForm(GatewayBackgroundService gatewayService)
    {
        InitializeComponent();

        _gatewayService = gatewayService;
        _gatewayService.BarcodeReceived += OnBarcodeReceived;
        _gatewayService.TaskSucceeded += OnTaskSucceeded;
        _gatewayService.TaskFailed += OnTaskFailed;

        _repository = MovingTaskRepository.Instance;

        Load += MainForm_Load;

    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        checkEditStatus.Properties.Caption = "Chưa kết nối";
        checkEditStatus.Checked = false;
        checkEditStatus.Enabled = false;
        checkEditStatus.ForeColor = Color.Gray;
        BtnConnect.Enabled = true;
        BtnDisconnect.Enabled = false;
        gridControl1.DataSource = _repository.GetAllTasks();

        // Disable inline editing
        gridView1.OptionsBehavior.Editable = false;

        // Setup grid columns
        SetupGridColumns();

        // Load ComboBox with CommandType enum using DescriptionAttribute
        LoadCommandTypes();

        // Event for ComboBox change
        CbCommand.SelectedIndexChanged += CbCommand_SelectedIndexChanged;

        // Default state (e.g., select first item)
        CbCommand.SelectedIndex = 0; // Inbound by default
        ResetLocationValues(); // Initial reset

        gridView1.FocusedRowChanged += GridView1_FocusedRowChanged;

        BtPause.Visible = false;
        BtResume.Visible = false;

        StartPollingLocation(true);
    }

    private Task StartPollingLocation(bool isPolling)
    {
        return Task.Run(async () =>
        {
            while (true)
            {
                if (!isPolling)
                {
                    Invoke(new Action(() =>
                    {
                        TbCurFloor.Value = 0;
                        TbCurRail.Value = 0;
                        TbCurBlock.Value = 0;
                        TbCountQueue.Value = 0;
                        CbIsResume.Checked = false;
                        TbTaskId.Text = "";
                    }));
                }
                try
                {
                    Location? location = await _gatewayService.GetActualLocationAsync(DeviceId)!;

                    if (location == null)
                    {
                        continue;
                    }

                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            TbCurFloor.Value = location.Floor;
                            TbCurRail.Value = location.Rail;
                            TbCurBlock.Value = location.Block;
                        }));
                    }
                    else
                    {
                        TbCurFloor.Value = location.Floor;
                        TbCurRail.Value = location.Rail;
                        TbCurBlock.Value = location.Block;
                    }

                    List<TransportTask> queue = [.. _gatewayService.GetPendingTask()];
                    string? taskId = _gatewayService.GetCurrentTask(DeviceId);

                    Invoke(new Action(() =>
                    {
                        TbCountQueue.Value = queue.Count;
                        CbIsResume.Checked = _gatewayService.IsPauseQueue;
                        TbTaskId.Text = taskId ?? "";
                    }));



                }
                catch (Exception ex)
                {
                    AppendLogToMemoEdit($"Error polling location: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        });
    }

    private void OnTaskFailed(object? sender, TaskFailedEventArgs e)
    {
        AppendLogToMemoEdit($"[{DateTime.Now}] Error: {e.ErrorDetail.ErrorMessage}");

        _repository.DeleteTask(e.TaskId);
        gridControl1.RefreshDataSource();

        Invoke(new Action(DisConnect));
        Invoke(new Action(HideButton));
    }

    private void OnTaskSucceeded(object? sender, TaskSucceededEventArgs e)
    {
        AppendLogToMemoEdit($"[{DateTime.Now}] Info:Thực hiện thành công Task {e.TaskId} trên {e.DeviceId}.");

        _repository.DeleteTask(e.TaskId);

        gridControl1.RefreshDataSource();

        Invoke(new Action(HideButton));
    }

    private void HideButton()
    {
        TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();

        if (taskQueueAfter.Length == 0)
        {
            BtResume.Visible = false;
            BtPause.Visible = false;
        }
    }

    private void OnBarcodeReceived(object? sender, BarcodeReceivedEventArgs e)
    {
        string logMessage = $"[{DateTime.Now}] Info: Nhận Barcode {e.Barcode} cửa Task {e.TaskId} cho {e.DeviceId}.";

        AppendLogToMemoEdit(logMessage);

        _repository.DeleteByBarcode(e.Barcode);

        gridControl1.RefreshDataSource();

        Invoke(new Action(HideButton));

    }

    private void AppendLogToMemoEdit(string message)
    {
        if (memoEdit1.InvokeRequired)
        {
            memoEdit1.BeginInvoke(new Action(() => AppendLogToMemoEdit(message)));
            return;
        }

        memoEdit1.Text += message + Environment.NewLine;

        memoEdit1.SelectionStart = memoEdit1.Text.Length;
        memoEdit1.ScrollToCaret();

        string[] lines = memoEdit1.Lines;
        if (lines.Length > 1000)
        {
            memoEdit1.Lines = [.. lines.Skip(1)];
        }
    }

    private async void BtnConnect_Click(object sender, EventArgs e)
    {
        try
        {
            await _gatewayService.ActivateDevice(DeviceId);

            string logMessage = $"[{DateTime.Now}] Info: {DeviceId} kết nối thành công.";

            AppendLogToMemoEdit(logMessage);

            checkEditStatus.Checked = true;
            checkEditStatus.Properties.Caption = "Đã kết nối";
            checkEditStatus.ForeColor = Color.Green;

            BtnConnect.Enabled = false;
            BtnDisconnect.Enabled = true;
        }
        catch (DeviceNotRegisteredException)
        {
            string logMessage = $"[{DateTime.Now}] Error: {DeviceId} chưa được đăng ký vào hệ thống.";

            AppendLogToMemoEdit(logMessage);
        }
        catch (PlcConnectionFailedException ex)
        {
            string logMessage = $"[{DateTime.Now}] Error: Kết nối đến {DeviceId} thất bại sau {ex.MaxRetries} thử lại!";
            AppendLogToMemoEdit(logMessage);
        }
        catch (Exception ex)
        {
            string logMessage = $"[{DateTime.Now}] Error: {ex.Message}";
            AppendLogToMemoEdit(logMessage);
            return;

        }
    }

    private void BtnDisconnect_Click(object sender, EventArgs e)
    {
        if (!checkEditStatus.Checked)
        {
            return;
        }

        DisConnect();

        StartPollingLocation(false);
    }

    private void DisConnect()
    {
        _gatewayService.DeactivateDevice(DeviceId);

        checkEditStatus.Properties.Caption = "Chưa kết nối";
        checkEditStatus.ForeColor = Color.Gray;

        BtnConnect.Enabled = true;
        BtnDisconnect.Enabled = false;

        string logMessage = $"[{DateTime.Now}] Info: {DeviceId} đóng kết nối thành công.";
        AppendLogToMemoEdit(logMessage);
    }

    private void BtRefresh_Click(object sender, EventArgs e)
    {
        ResetLocationValues();
        gridControl1.RefreshDataSource();
    }

    private void BtAddCommand_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(TbGateNumber.Text))
        {
            XtraMessageBox.Show("Vui lòng nhập đầy đủ thông tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (CbCommand.SelectedItem is ImageComboBoxItem selectedItem)
        {
            CommandType cmdType = (CommandType)selectedItem.Value;

            Location? srcLoc = null;
            if (TbSrcFloor.Enabled)
                srcLoc = new Location((short)TbSrcFloor.Value, (short)TbSrcRail.Value, (short)TbSrcBlock.Value);

            Location? targetLoc = null;
            if (TbTargetFloor.Enabled)
                targetLoc = new Location((short)TbTargetFloor.Value, (short)TbTargetRail.Value, (short)TbTargetBlock.Value);

            string newTaskId = "TASK_" + (_repository.Count + 1).ToString("D3");

            Direction inDir = ToggleInDir.IsOn ? Direction.Top : Direction.Bottom;
            Direction outDir = ToggleOutDir.IsOn ? Direction.Top : Direction.Bottom;
            string barcode = ((int)TbBarcode.Value).ToString("D10");

            var newTask = new MovingTask
            {
                TaskId = newTaskId,
                CommandType = cmdType,
                Barcode = cmdType == CommandType.Inbound ? barcode : "",
                SourceLocation = srcLoc,
                TargetLocation = targetLoc,
                GateNumber = (short)TbGateNumber.Value,
                InDirBlock = inDir,
                OutDirBlock = outDir
            };

            _repository.AddTask(newTask);

            ResetLocationValues();
            CbCommand.SelectedIndex = 0;
            gridControl1.RefreshDataSource();
            BtExecute.Visible = true;

            string logMessage = $"[{DateTime.Now}] Info: Thêm Task {newTaskId} loại {cmdType} thành công.";
            AppendLogToMemoEdit(logMessage);
        }
    }

    private void CbCommand_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (CbCommand.SelectedItem is ImageComboBoxItem selectedItem)
        {
            CommandType selectedType = (CommandType)selectedItem.Value;

            bool enableSrc = false;
            bool enableTarget = false;

            switch (selectedType)
            {
                case CommandType.Inbound:
                    enableSrc = false;
                    enableTarget = true;
                    break;
                case CommandType.Outbound:
                    enableSrc = true;
                    enableTarget = false;
                    break;
                case CommandType.Transfer:
                    enableSrc = true;
                    enableTarget = true;
                    break;
            }

            // Apply to Src controls
            TbSrcFloor.Enabled = enableSrc;
            TbSrcRail.Enabled = enableSrc;
            TbSrcBlock.Enabled = enableSrc;

            // Apply to Target controls
            TbTargetFloor.Enabled = enableTarget;
            TbTargetRail.Enabled = enableTarget;
            TbTargetBlock.Enabled = enableTarget;
            TbBarcode.Enabled = enableTarget;
        }
    }

    private void ResetLocationValues()
    {
        TbSrcFloor.Value = 1;
        TbSrcRail.Value = 1;
        TbSrcBlock.Value = 5;

        TbTargetFloor.Value = 1;
        TbTargetRail.Value = 1;
        TbTargetBlock.Value = 5;

        TbBarcode.Value = 1;

        ToggleInDir.Enabled = false;
        ToggleOutDir.Enabled = false;
        TbGateNumber.Enabled = false;
        TbCurBlock.Enabled = false;
        TbCurFloor.Enabled = false;
        TbCurRail.Enabled = false;
    }

    private void LoadCommandTypes()
    {
        foreach (CommandType cmd in Enum.GetValues(typeof(CommandType)))
        {
            CbCommand.Properties.Items.Add(new ImageComboBoxItem(GetDescription(cmd), cmd));
        }
    }

    public static string GetDescription(Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString())!;
        DescriptionAttribute attribute = (DescriptionAttribute)field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault()!;
        return attribute == null ? value.ToString() : attribute.Description;
    }

    private void SetupGridColumns()
    {
        gridView1.Columns["SourceLocation"].Visible = false;
        gridView1.Columns["TargetLocation"].Visible = false;

        gridView1.OptionsView.ColumnAutoWidth = true;
        gridView1.OptionsBehavior.AutoExpandAllGroups = false;

        gridControl1.AutoSize = true;

        var colSrcFloor = new GridColumn { FieldName = "SourceLocation.Floor", Caption = "Từ Floor", Visible = true };
        var colSrcRail = new GridColumn { FieldName = "SourceLocation.Rail", Caption = "Từ Rail", Visible = true };
        var colSrcBlock = new GridColumn { FieldName = "SourceLocation.Block", Caption = "Từ Block", Visible = true };

        var colTargetFloor = new GridColumn { FieldName = "TargetLocation.Floor", Caption = "Đến Floor", Visible = true };
        var colTargetRail = new GridColumn { FieldName = "TargetLocation.Rail", Caption = "Đến Rail", Visible = true };
        var colTargetBlock = new GridColumn { FieldName = "TargetLocation.Block", Caption = "Đến Block", Visible = true };

        gridView1.Columns.AddRange([colSrcFloor, colSrcRail, colSrcBlock, colTargetFloor, colTargetRail, colTargetBlock]);

        var repoCommand = new RepositoryItemImageComboBox();

        foreach (CommandType cmd in Enum.GetValues(typeof(CommandType)))
        {
            repoCommand.Items.Add(new ImageComboBoxItem(GetDescription(cmd), cmd, -1));
        }
        gridView1.Columns["CommandType"].ColumnEdit = repoCommand;
        gridView1.Columns["CommandType"].Caption = "Lệnh";

        var repoDirection = new RepositoryItemImageComboBox();
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            repoDirection.Items.Add(new ImageComboBoxItem(dir.ToString(), dir, -1));
        }
        gridView1.Columns["InDirBlock"].ColumnEdit = repoDirection;
        gridView1.Columns["InDirBlock"].Caption = "Vào";
        gridView1.Columns["OutDirBlock"].ColumnEdit = repoDirection;
        gridView1.Columns["OutDirBlock"].Caption = "Ra";
        gridView1.Columns["GateNumber"].Caption = "Cửa";
    }
    private void GridView1_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
    {
        var row = gridView1.GetFocusedRow();
        _selectedTask = row as TransportTask;
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        bool isPause = _gatewayService.IsPauseQueue;
        if (!isPause)
        {
            XtraMessageBox.Show("Vui lòng đóng hàng đợi trước khi xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return;
        }

        if (_selectedTask == null)
        {
            XtraMessageBox.Show("Vui lòng chọn một lệnh để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string confirmMessage = $"Bạn có chắc chắn muốn xóa {_selectedTask.TaskId}?";
        if (XtraMessageBox.Show(confirmMessage, "Xác nhận xóa", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
        {
            _repository.DeleteTask(_selectedTask.TaskId);
            _gatewayService.RemoveTransportTasks([_selectedTask.TaskId]);
            gridControl1.RefreshDataSource();
            _selectedTask = null;

            string logMessage = $"[{DateTime.Now}] Info: Xóa task {_selectedTask?.TaskId} thành công.";
            AppendLogToMemoEdit(logMessage);
        }

        Invoke(new Action(HideButton));

    }

    private void BtnDeleteAll_Click(object sender, EventArgs e)
    {
        bool isPause = _gatewayService.IsPauseQueue;
        if (!isPause)
        {
            XtraMessageBox.Show("Vui lòng đóng hàng đợi trước khi xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string confirmMessage = $"Bạn có chắc chắn muốn xóa tất cả các lệnh?";
        if (XtraMessageBox.Show(confirmMessage, "Xác nhận xóa", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
        {
            var tasks = _repository.GetAll().Select(x => x.TaskId);
            _gatewayService.RemoveTransportTasks(tasks);

            _repository.DeleteAllTasks();
            gridControl1.RefreshDataSource();
            _selectedTask = null;

            string logMessage = $"[{DateTime.Now}] Info: Tất cả task đã được xóa.";
            AppendLogToMemoEdit(logMessage);
        }

        Invoke(new Action(HideButton));
    }

    private async void BtExecute_Click(object sender, EventArgs e)
    {
        try
        {
            List<MovingTask> commands = _repository.GetAll();

            if (commands.Count == 0)
            {
                XtraMessageBox.Show("Chưa có lệnh nào để chạy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = commands.Select(x => new TransportTask
            {
                TaskId = x.TaskId,
                CommandType = x.CommandType,
                GateNumber = x.GateNumber,
                InDirBlock = x.InDirBlock,
                OutDirBlock = x.OutDirBlock,
                SourceLocation = x.SourceLocation,
                TargetLocation = x.TargetLocation,
            }).ToList();

            await _gatewayService.SendMultipleCommands(results);

            BtExecute.Visible = false;
            BtPause.Visible = true;
        }
        catch (Exception ex)
        {
            string logMessage = $"[{DateTime.Now}] Error: Gửi lệnh thất bại, Lỗi {ex.Message}.";
            AppendLogToMemoEdit(logMessage);
        }

    }

    private void BtPause_Click(object sender, EventArgs e)
    {
        BtResume.Visible = true;
        BtPause.Visible = false;

        _gatewayService.PauseQueue();
    }

    private void BtResume_Click(object sender, EventArgs e)
    {
        BtResume.Visible = false;

        _gatewayService.ResumeQueue();

        if (_repository.Count > 0)
        {
            BtPause.Visible = true;
        }
        else
        {
            BtExecute.Visible = true;
        }
    }

    private void BtQueue_Click(object sender, EventArgs e)
    {
        try
        {
            List<TransportTask> queue = [.. _gatewayService.GetPendingTask()];
            CbIsResume.Checked = _gatewayService.IsPauseQueue;
            Invoke(new Action(() =>
            {
                TbCountQueue.Value = queue.Count;
                CbIsResume.Checked = _gatewayService.IsPauseQueue;
            }));

        }
        catch (Exception ex)
        {
            AppendLogToMemoEdit($"[{DateTime.Now}] Error: Đọc queue thất bại: {ex.Message}");
        }
    }
}