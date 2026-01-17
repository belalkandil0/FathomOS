using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for milestone add/edit dialog with status workflow
/// </summary>
public class MilestoneViewModel : ViewModelBase
{
    private readonly ProjectMilestone _original;
    private readonly bool _isNew;

    public MilestoneViewModel(ProjectMilestone? milestone = null)
    {
        _isNew = milestone == null;
        _original = milestone ?? new ProjectMilestone();

        // Copy values for editing
        Milestone = new ProjectMilestone
        {
            MilestoneId = _original.MilestoneId,
            ProjectId = _original.ProjectId,
            Name = _original.Name,
            Description = _original.Description,
            Type = _original.Type,
            Status = _original.Status,
            PlannedDate = _original.PlannedDate,
            ActualDate = _original.ActualDate,
            Priority = _original.Priority,
            IsPaymentMilestone = _original.IsPaymentMilestone,
            OwnerName = _original.OwnerName,
            Notes = _original.Notes,
            AcceptanceCriteria = _original.AcceptanceCriteria,
            SortOrder = _original.SortOrder,
            IsActive = _original.IsActive
        };

        // Initialize collections
        MilestoneTypes = new ObservableCollection<MilestoneType>();
        MilestoneStatuses = new ObservableCollection<MilestoneStatus>();

        foreach (MilestoneType type in Enum.GetValues(typeof(MilestoneType)))
            MilestoneTypes.Add(type);
        foreach (MilestoneStatus status in Enum.GetValues(typeof(MilestoneStatus)))
            MilestoneStatuses.Add(status);

        // Initialize commands
        SaveCommand = new RelayCommand(_ => Save(null), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel(null));
        CompleteCommand = new RelayCommand(_ => Complete(null), _ => CanComplete());
    }

    #region Properties

    public ProjectMilestone Milestone { get; }

    public string Title => _isNew ? "Add Milestone" : "Edit Milestone";

    public ObservableCollection<MilestoneType> MilestoneTypes { get; }
    public ObservableCollection<MilestoneStatus> MilestoneStatuses { get; }

    public string Name
    {
        get => Milestone.Name;
        set
        {
            if (Milestone.Name != value)
            {
                Milestone.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Description
    {
        get => Milestone.Description;
        set
        {
            if (Milestone.Description != value)
            {
                Milestone.Description = value;
                OnPropertyChanged();
            }
        }
    }

    public MilestoneType SelectedType
    {
        get => Milestone.Type;
        set
        {
            if (Milestone.Type != value)
            {
                Milestone.Type = value;
                OnPropertyChanged();
            }
        }
    }

    public MilestoneStatus SelectedStatus
    {
        get => Milestone.Status;
        set
        {
            if (Milestone.Status != value)
            {
                Milestone.Status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanComplete));
            }
        }
    }

    public DateTime? PlannedDate
    {
        get => Milestone.PlannedDate;
        set
        {
            if (Milestone.PlannedDate != value)
            {
                Milestone.PlannedDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ActualDate
    {
        get => Milestone.ActualDate;
        set
        {
            if (Milestone.ActualDate != value)
            {
                Milestone.ActualDate = value;
                OnPropertyChanged();
            }
        }
    }

    public PriorityLevel SelectedPriority
    {
        get => Milestone.Priority;
        set
        {
            if (Milestone.Priority != value)
            {
                Milestone.Priority = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPaymentMilestone
    {
        get => Milestone.IsPaymentMilestone;
        set
        {
            if (Milestone.IsPaymentMilestone != value)
            {
                Milestone.IsPaymentMilestone = value;
                OnPropertyChanged();
            }
        }
    }

    public string? OwnerName
    {
        get => Milestone.OwnerName;
        set
        {
            if (Milestone.OwnerName != value)
            {
                Milestone.OwnerName = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Notes
    {
        get => Milestone.Notes;
        set
        {
            if (Milestone.Notes != value)
            {
                Milestone.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    public string? AcceptanceCriteria
    {
        get => Milestone.AcceptanceCriteria;
        set
        {
            if (Milestone.AcceptanceCriteria != value)
            {
                Milestone.AcceptanceCriteria = value;
                OnPropertyChanged();
            }
        }
    }

    public int SortOrder
    {
        get => Milestone.SortOrder;
        set
        {
            if (Milestone.SortOrder != value)
            {
                Milestone.SortOrder = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CompleteCommand { get; }

    #endregion

    #region Events

    public event EventHandler<ProjectMilestone>? SaveCompleted;
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

        Milestone.UpdatedAt = DateTime.UtcNow;
        if (_isNew)
        {
            Milestone.CreatedAt = DateTime.UtcNow;
        }

        SaveCompleted?.Invoke(this, Milestone);
    }

    private void Cancel(object? parameter)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanComplete()
    {
        return Milestone.Status != MilestoneStatus.Completed;
    }

    private void Complete(object? parameter)
    {
        SelectedStatus = MilestoneStatus.Completed;
        ActualDate = DateTime.Now;
    }

    #endregion
}
