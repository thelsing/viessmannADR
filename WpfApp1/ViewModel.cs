using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class ViewModel : INotifyPropertyChanged
    {
        ViessmannEntities _model;
        public ViewModel()
        {
            _model = new ViessmannEntities();
            _model.DatapointTypeGroups.OrderBy(_ => _.OrderIndex).ToList().ForEach(_ => DatapointTypeGroups.Add(_));
        }

        public ObservableCollection<DatapointTypeGroup> DatapointTypeGroups { get; set; } = new ObservableCollection<DatapointTypeGroup>();

        private DatapointTypeGroup _selectedDatapointTypeGroup;
        public DatapointTypeGroup SelectedDatapointTypeGroup
        {
            get => _selectedDatapointTypeGroup;
            set
            {
                if (_selectedDatapointTypeGroup == value)
                    return;
                _selectedDatapointTypeGroup = value;
                OnPropertyChanged();

                DatapointTypes = _selectedDatapointTypeGroup?.DatapointTypes;
            }

        }

        private ObservableCollection<DatapointType> _datapointTypes;
        public ObservableCollection<DatapointType> DatapointTypes
        {
            get => _datapointTypes;
            set
            {
                if (_datapointTypes == value)
                    return;
                _datapointTypes = value;
                OnPropertyChanged();
            }
        }
        private DatapointType _selectedDatapointType;
        public DatapointType SelectedDatapointType
        {
            get => _selectedDatapointType;
            set
            {
                if (_selectedDatapointType == value)
                    return;
                _selectedDatapointType = value;
                OnPropertyChanged();

                if (_selectedDatapointType == null)
                    EventTypeGroups = null;
                else
                    EventTypeGroups = new ObservableCollection<EventTypeGroup>(
                        _selectedDatapointType?.EventTypeGroups.Where(_ => _.ParentId == -1
                            && (_.ChildEventTypeGroups.Any() || _.EventTypeLinks.Any())).OrderBy(_ => _.OrderIndex));
            }

        }

        private ObservableCollection<EventTypeGroup> _eventTypeGroups;
        public ObservableCollection<EventTypeGroup> EventTypeGroups
        {
            get => _eventTypeGroups;
            set
            {
                if (_eventTypeGroups == value)
                    return;
                _eventTypeGroups = value;
                OnPropertyChanged();
            }
        }
        private EventTypeGroup _selectedEventTypeGroup;
        public EventTypeGroup SelectedEventTypeGroup
        {
            get => _selectedEventTypeGroup;
            set
            {
                if (_selectedEventTypeGroup == value)
                    return;
                _selectedEventTypeGroup = value;
                OnPropertyChanged();
                if (_selectedEventTypeGroup == null)
                    EventTypes = null;
                else
                    EventTypes = new ObservableCollection<EventType>(
                        _selectedEventTypeGroup?.EventTypeLinks?.OrderBy(_ => _.EventTypeOrder).Select(_ => _.EventType)
                        );
            }

        }

        private ObservableCollection<EventType> _eventTypes;
        public ObservableCollection<EventType> EventTypes
        {
            get => _eventTypes;
            set
            {
                if (_eventTypes == value)
                    return;
                _eventTypes = value;
                OnPropertyChanged();
            }
        }
        private EventType _selectedEventType;
        public EventType SelectedEventType
        {
            get => _selectedEventType;
            set
            {
                if (_selectedEventType == value)
                    return;
                _selectedEventType = value;
                OnPropertyChanged();
                EventValueTypes = _selectedEventType?.EventValueTypes;
            }

        }

        private ObservableCollection<EventValueType> _eventValueTypes;
        public ObservableCollection<EventValueType> EventValueTypes
        {
            get => _eventValueTypes;
            set
            {
                if (_eventValueTypes == value)
                    return;
                _eventValueTypes = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
