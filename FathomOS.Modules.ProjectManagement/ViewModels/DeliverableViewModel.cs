using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for deliverable add/edit dialog with tracking
/// </summary>
public class DeliverableViewModel : ViewModelBase
{
    private readonly ProjectDeliverable _original;
    private readonly bool _isNew;

    public DeliverableViewModel(ProjectDeliverable? deliverable = null)
    {
        _isNew = deliverable == null;
        _original = deliverable ?? new ProjectDeliverable();

        // Copy values for editing
        Deliverable = new ProjectDeliverable
        {
            DeliverableId = _original.DeliverableId,
            ProjectId = _original.ProjectId,
            MilestoneId = _original.MilestoneId,
            Name = _original.Name,
            Description = _original.Description,
            Type = _original.Type,
            Format = _original.Format,
            Status = _original.Status,
            PlannedDueDate = _original.PlannedDueDate,
            ForecastDate = _original.ForecastDate,
            SubmissionDate = _original.SubmissionDate,
            ApprovalDate = _original.ApprovalDate,
            RevisionNumber = _original.RevisionNumber,
            OwnerName = _original.OwnerName,
            Notes = _original.Notes,
            RejectionReason = _original.RejectionReason,
            SortOrder = _original.SortOrder,
            IsActive = _original.IsActive
        };

        // Initialize collections
        DeliverableTypes = new ObservableCollection<DeliverableType>();
        DeliverableStatuses = new ObservableCollection<DeliverableStatus>();
        DeliverableFormats = new ObservableCollection<DeliverableFormat>();

        foreach (DeliverableType type in Enum.GetValues(typeof(DeliverableType)))
            DeliverableTypes.Add(type);
        foreach (DeliverableStatus status in Enum.GetValues(typeof(DeliverableStatus)))
            DeliverableStatuses.Add(status);
        foreach (DeliverableFormat format in Enum.GetValues(typeof(DeliverableFormat)))
            DeliverableFormats.Add(format);

        // Initialize commands
        SaveCommand = new RelayCommand(_ => Save(null), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel(null));
        SubmitCommand = new RelayCommand(_ => Submit(null), _ => CanSubmit);
    }

    #region Properties

    public ProjectDeliverable Deliverable { get; }

    public string Title => _isNew ? "Add Deliverable" : "Edit Deliverable";

    public ObservableCollection<DeliverableType> DeliverableTypes { get; }
    public ObservableCollection<DeliverableStatus> DeliverableStatuses { get; }
    public ObservableCollection<DeliverableFormat> DeliverableFormats { get; }

    public string Name
    {
        get => Deliverable.Name;
        set
        {
            if (Deliverable.Name != value)
            {
                Deliverable.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Description
    {
        get => Deliverable.Description;
        set
        {
            if (Deliverable.Description != value)
            {
                Deliverable.Description = value;
                OnPropertyChanged();
            }
        }
    }

    public DeliverableType SelectedType
    {
        get => Deliverable.Type;
        set
        {
            if (Deliverable.Type != value)
            {
                Deliverable.Type = value;
                OnPropertyChanged();
            }
        }
    }

    public DeliverableFormat SelectedFormat
    {
        get => Deliverable.Format;
        set
        {
            if (Deliverable.Format != value)
            {
                Deliverable.Format = value;
                OnPropertyChanged();
            }
        }
    }

    public DeliverableStatus SelectedStatus
    {
        get => Deliverable.Status;
        set
        {
            if (Deliverable.Status != value)
            {
                Deliverable.Status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    public DateTime? PlannedDueDate
    {
        get => Deliverable.PlannedDueDate;
        set
        {
            if (Deliverable.PlannedDueDate != value)
            {
                Deliverable.PlannedDueDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ForecastDate
    {
        get => Deliverable.ForecastDate;
        set
        {
            if (Deliverable.ForecastDate != value)
            {
                Deliverable.ForecastDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? SubmissionDate
    {
        get => Deliverable.SubmissionDate;
        set
        {
            if (Deliverable.SubmissionDate != value)
            {
                Deliverable.SubmissionDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ApprovalDate
    {
        get => Deliverable.ApprovalDate;
        set
        {
            if (Deliverable.ApprovalDate != value)
            {
                Deliverable.ApprovalDate = value;
                OnPropertyChanged();
            }
        }
    }

    public string? RevisionNumber
    {
        get => Deliverable.RevisionNumber;
        set
        {
            if (Deliverable.RevisionNumber != value)
            {
                Deliverable.RevisionNumber = value;
                OnPropertyChanged();
            }
        }
    }

    public string? OwnerName
    {
        get => Deliverable.OwnerName;
        set
        {
            if (Deliverable.OwnerName != value)
            {
                Deliverable.OwnerName = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Notes
    {
        get => Deliverable.Notes;
        set
        {
            if (Deliverable.Notes != value)
            {
                Deliverable.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    public string? RejectionReason
    {
        get => Deliverable.RejectionReason;
        set
        {
            if (Deliverable.RejectionReason != value)
            {
                Deliverable.RejectionReason = value;
                OnPropertyChanged();
            }
        }
    }

    public int SortOrder
    {
        get => Deliverable.SortOrder;
        set
        {
            if (Deliverable.SortOrder != value)
            {
                Deliverable.SortOrder = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanSubmit => Deliverable.Status != DeliverableStatus.Submitted &&
                              Deliverable.Status != DeliverableStatus.Accepted;

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SubmitCommand { get; }

    #endregion

    #region Events

    public event EventHandler<ProjectDeliverable>? SaveCompleted;
    public event EventHandler? CancelRequested;

    #endregion

    #region Methods

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private void Save(object? parameter)
    {
        if (!CanSave()) return;

        Deliverable.UpdatedAt = DateTime.UtcNow;
        if (_isNew)
        {
            Deliverable.CreatedAt = DateTime.UtcNow;
        }

        SaveCompleted?.Invoke(this, Deliverable);
    }

    private void Cancel(object? parameter)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Submit(object? parameter)
    {
        SelectedStatus = DeliverableStatus.Submitted;
        SubmissionDate = DateTime.Now;
    }

    #endregion
}
