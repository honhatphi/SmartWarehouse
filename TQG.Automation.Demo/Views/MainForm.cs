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
    private MovingTask? _selectedTask;
    private const string DeviceId = "Shuttle01";

    public MainForm(GatewayBackgroundService gatewayService)
    {
        InitializeComponent();

        _gatewayService = gatewayService;
        _gatewayService.BarcodeReceived += OnBarcodeReceived;
        _gatewayService.TaskSucceeded += OnTaskSucceeded;
        _gatewayService.TaskFailed += OnTaskFailed;
        _gatewayService.BarcodeExecute += OnExecute;

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

        BtExecute.Visible = false;
        BtPause.Visible = false;
        BtnDelete.Visible = false;
        BtnDeleteAll.Visible = false;
        BtReset.Visible = false;

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
                    List<TransportTask> queue = [.. _gatewayService.GetPendingTask()];
                    string? taskId = _gatewayService.GetCurrentTask(DeviceId);

                    Invoke(new Action(() =>
                    {
                        TbCountQueue.Value = queue.Count;
                        CbIsResume.Checked = _gatewayService.IsPauseQueue;
                        TbTaskId.Text = taskId ?? "";
                    }));

                    Location? location = await _gatewayService.GetActualLocationAsync(DeviceId)!;

                    if (location == null)
                    {
                        continue;
                    }

                    Invoke(new Action(() =>
                    {
                        TbCurFloor.Value = location.Floor;
                        TbCurRail.Value = location.Rail;
                        TbCurBlock.Value = location.Block;
                    }));
                }
                catch (Exception ex)
                {
                    AppendLogToMemoEdit($"Error polling location: {ex.Message}");
                }

                await Task.Delay(500);
            }
        });
    }

    private void OnTaskFailed(object? sender, TaskFailedEventArgs e)
    {
        AppendLogToMemoEdit($"[{DateTime.Now}] Error: {e.ErrorDetail.ErrorMessage}");

        _repository.DeleteTask(e.TaskId);
        gridControl1.RefreshDataSource();

        Invoke(new Action(() =>
        {
            BtPause.Visible = false;
            TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();

            BtReset.Visible = true;

            if (taskQueueAfter.Length > 0)
            {
                BtnDelete.Visible = true;
                BtnDeleteAll.Visible = true;
                BtExecute.Visible = true;
            }
            else
            {
                BtnDelete.Visible = false;
                BtnDeleteAll.Visible = false;
                BtExecute.Visible = false;
            }
        }));
    }

    private void OnTaskSucceeded(object? sender, TaskSucceededEventArgs e)
    {
        AppendLogToMemoEdit($"[{DateTime.Now}] Info:Thực hiện thành công Task {e.TaskId} trên {e.DeviceId}.");

        _repository.DeleteTask(e.TaskId);

        gridControl1.RefreshDataSource();

        Invoke(new Action(() =>
        {
            TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();

            if (taskQueueAfter.Length > 0)
            {
                BtPause.Visible = true;
                BtnDelete.Visible = false;
                BtnDeleteAll.Visible = false;
                BtExecute.Visible = false;
            }
            else
            {
                BtPause.Visible = false;
                BtExecute.Visible = false;
                BtnDelete.Visible = false;
                BtnDeleteAll.Visible = false;
            }
        }));
    }

    private void OnBarcodeReceived(object? sender, BarcodeReceivedEventArgs e)
    {
        string logMessage = $"[{DateTime.Now}] Info: Nhận Barcode [{e.Barcode}] Task [{e.TaskId}] cho {e.DeviceId}.";

        AppendLogToMemoEdit(logMessage);
    }

    private void OnExecute(object? sender, BarcodeExecuteEventArgs e)
    {
        string status = e.IsValid ? "hợp lệ" : "không hợp lệ";
        string logMessage = $"[{DateTime.Now}] Info: Xác thực Barcode [{e.Barcode}] {status}.";
        AppendLogToMemoEdit(logMessage);

        if (e.IsValid && e.TargetLocation != null)
        {
            string locationMessage = $"[{DateTime.Now}] Info: Nhập Barcode [{e.Barcode}] đến vị trí Block: {e.TargetLocation.Block}, Floor {e.TargetLocation.Floor}, Rail {e.TargetLocation.Rail}";
            AppendLogToMemoEdit(locationMessage);
        }
    }

    private void AppendLogToMemoEdit(string message)
    {
        Invoke(new Action(() =>
        {
            memoEdit1.Text += message + Environment.NewLine;

            memoEdit1.SelectionStart = memoEdit1.Text.Length;
            memoEdit1.ScrollToCaret();

            string[] lines = memoEdit1.Lines;
            if (lines.Length > 1000)
            {
                memoEdit1.Lines = [.. lines.Skip(1)];
            }
        }));
    }

    private async void BtnConnect_Click(object sender, EventArgs e)
    {
        try
        {
            await _gatewayService.ActivateDevice(DeviceId);

            string logMessage = $"[{DateTime.Now}] Info: {DeviceId} kết nối thành công.";

            AppendLogToMemoEdit(logMessage);

            Invoke(new Action(() =>
            {
                checkEditStatus.Checked = true;
                checkEditStatus.Properties.Caption = "Đã kết nối";
                checkEditStatus.ForeColor = Color.Green;

                BtnConnect.Enabled = false;
                BtnDisconnect.Enabled = true;

            }));

            await StartPollingLocation(true);
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

        _gatewayService.DeactivateDevice(DeviceId);

        Invoke(new Action(() =>
        {
            checkEditStatus.Properties.Caption = "Chưa kết nối";
            checkEditStatus.ForeColor = Color.Gray;

            BtnConnect.Enabled = true;
            BtnDisconnect.Enabled = false;

        }));

        string logMessage = $"[{DateTime.Now}] Info: {DeviceId} đóng kết nối thành công.";
        AppendLogToMemoEdit(logMessage);

        StartPollingLocation(false);
    }

    private void BtRefresh_Click(object sender, EventArgs e)
    {
        ResetLocationValues();
        gridControl1.RefreshDataSource();
    }

    private async void BtAddCommand_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(TbGateNumber.Text))
        {
            XtraMessageBox.Show("Vui lòng nhập đầy đủ thông tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if(TbSrcBlock.Value == 4 || TbTargetBlock.Value == 4)
        {
            XtraMessageBox.Show("Block là 3 hoặc 5.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string barcode = TbBarcode.Value.ToString();

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

            Invoke(new Action(() =>
            {
                ResetLocationValues();
                CbCommand.SelectedIndex = 0;
                gridControl1.RefreshDataSource();

                BtExecute.Visible = true;
                BtnDelete.Visible = true;
                BtnDeleteAll.Visible = true;
                BtPause.Visible = false;
            }));

            var command = new TransportTask
            {
                TaskId = newTask.TaskId,
                CommandType = newTask.CommandType,
                GateNumber = newTask.GateNumber,
                InDirBlock = newTask.InDirBlock,
                OutDirBlock = newTask.OutDirBlock,
                SourceLocation = newTask.SourceLocation,
                TargetLocation = newTask.TargetLocation,
            };

            await _gatewayService.SendMultipleCommands([command]);

            string type = cmdType switch
            {
                CommandType.Inbound => "Nhập hàng",
                CommandType.Outbound => "Xuất hàng",
                _ => "Chuyển vị trí"
            };

            string logMessage = $"[{DateTime.Now}] Info: Thêm {newTaskId} [Lệnh {type}] thành công.";


            if (cmdType == CommandType.Inbound)
            {
                logMessage = $"[{DateTime.Now}] Info: Thêm {newTaskId} [Lệnh {type}] đến vị trí " +
                    $"Block {newTask.TargetLocation!.Block}, Floor {newTask.TargetLocation!.Floor}, Rail {newTask.TargetLocation!.Rail} thành công.";
            }
            else if (cmdType == CommandType.Outbound)
            {
                logMessage = $"[{DateTime.Now}] Info: Thêm {newTaskId} [Lệnh {type}] từ vị trí " +
                    $"Block {newTask.SourceLocation!.Block}, Floor {newTask.SourceLocation!.Floor}, Rail {newTask.SourceLocation!.Rail} thành công.";
            }
            else
            {
                logMessage = $"[{DateTime.Now}] Info: Thêm {newTaskId} [Lệnh {type}] từ vị trí " +
                    $"Block {newTask.SourceLocation!.Block}, Floor {newTask.SourceLocation!.Floor}, Rail {newTask.SourceLocation!.Rail} " +
                    $"đến vị trí Block {newTask.TargetLocation!.Block}, Floor {newTask.TargetLocation!.Floor}, Rail {newTask.TargetLocation!.Rail} thành công.";
            }

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

            if (selectedType == CommandType.Inbound)
            {
                TbBarcode.Enabled = true;
            }
            else
            {
                TbBarcode.Enabled = false;
            }


            ResetLocationValues();
        }
    }

    private void ResetLocationValues()
    {
        TbSrcFloor.Value = 1;
        TbSrcRail.Value = 1;
        TbSrcBlock.Value = 3;

        TbTargetFloor.Value = 1;
        TbTargetRail.Value = 1;
        TbTargetBlock.Value = 3;

        TbBarcode.Value = 105;

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
        _selectedTask = row as MovingTask;
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedTask == null)
        {
            XtraMessageBox.Show("Vui lòng chọn một lệnh để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string confirmMessage = $"Bạn có chắc chắn muốn xóa {_selectedTask.TaskId}?";
        if (XtraMessageBox.Show(confirmMessage, "Xác nhận xóa", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
        {
            _gatewayService.RemoveTransportTasks([_selectedTask.TaskId]);
            _repository.DeleteTask(_selectedTask.TaskId);
            gridControl1.RefreshDataSource();
            _selectedTask = null;

            string logMessage = $"[{DateTime.Now}] Info: Xóa task {_selectedTask?.TaskId} thành công.";
            AppendLogToMemoEdit(logMessage);

            Invoke(new Action(() =>
            {
                TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();

                if (taskQueueAfter.Length == 0)
                {
                    BtPause.Visible = false;
                    BtExecute.Visible = false;
                    BtnDelete.Visible = false;
                    BtnDeleteAll.Visible = false;
                }
            }));
        }
    }

    private void BtnDeleteAll_Click(object sender, EventArgs e)
    {
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

            Invoke(new Action(() =>
            {
                BtPause.Visible = false;
                BtExecute.Visible = false;
                BtnDelete.Visible = false;
                BtnDeleteAll.Visible = false;
            }));
        }
    }

    private void BtExecute_Click(object sender, EventArgs e)
    {
        if (!checkEditStatus.Checked)
        {
            XtraMessageBox.Show("Kết nối trước khi chạy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (BtReset.Visible)
        {
            XtraMessageBox.Show("Reset trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Invoke(new Action(() =>
        {
            TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();

            BtExecute.Visible = false;
            BtnDelete.Visible = false;
            BtnDeleteAll.Visible = false;
            BtPause.Visible = true;

            if (taskQueueAfter.Length > 0)
            {
                _gatewayService.ResumeQueue();
            }
            else
            {
                XtraMessageBox.Show("Thêm lệnh trước khi chạy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }));
    }

    private void BtPause_Click(object sender, EventArgs e)
    {
        Invoke(new Action(() =>
        {
            TransportTask[] taskQueueAfter = _gatewayService.GetPendingTask();
            if (taskQueueAfter.Length > 0)
            {
                BtExecute.Visible = true;
                BtnDelete.Visible = true;
                BtnDeleteAll.Visible = true;
            }

            BtPause.Visible = false;
            _gatewayService.PauseQueue();
        }));
    }

    private void BtReset_Click(object sender, EventArgs e)
    {
        Invoke(new Action(() =>
        {
            BtReset.Visible = false;
        }));

        _gatewayService.ResetDeviceStatus(DeviceId);
    }
}