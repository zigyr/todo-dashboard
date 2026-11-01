using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TodoDashboard.Models;
using TodoDashboard.Services;

namespace TodoDashboard.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    private readonly HistoryStore _store;
    private readonly Func<Task<string?>> _pickCsv;
    private HistoryEntry? _selectedEntry;
    private string? _notice;

    public HistoryViewModel(HistoryStore store, Func<Task<string?>> pickCsv)
    {
        _store = store;
        _pickCsv = pickCsv;
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !IsBusy);
        OpenFolderCommand = new RelayCommand(_ => OpenFolder());
        CopySelectedUrlCommand = new RelayCommand(_ => CopySelectedUrl());

        Entries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(CountText));
        };
    }

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    public HistoryEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                OnPropertyChanged(nameof(HasSelected));
            }
        }
    }

    public bool IsBusy { get; private set; }
    public bool HasEntries => Entries.Count > 0;
    public bool IsEmpty => !HasEntries;
    public bool HasSelected => SelectedEntry is not null;
    public string CountText => $"{Entries.Count} 条";
    public string CsvPath => _store.FilePath;
    public string CsvFolder => Path.GetDirectoryName(_store.FilePath) ?? "";

    public string? Notice
    {
        get => _notice;
        private set
        {
            if (SetProperty(ref _notice, value))
            {
                OnPropertyChanged(nameof(HasNotice));
            }
        }
    }

    public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);

    public AsyncRelayCommand ImportCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand CopySelectedUrlCommand { get; }

    public async Task InitializeAsync()
    {
        var entries = await _store.LoadAsync();
        Replace(entries, selectFirst: true);
    }

    public async Task AddAsync(HistoryEntry entry)
    {
        Entries.Insert(0, entry);
        SelectedEntry = entry;
        await _store.SaveAllAsync(Entries);
    }

    private async Task ImportAsync()
    {
        var path = await _pickCsv();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var imported = await _store.LoadFromAsync(path);
            Replace(imported, selectFirst: true);
            await _store.SaveAllAsync(Entries);
            Notice = $"已导入 {imported.Count} 条，并覆盖本地 CSV。";
        }
        catch (Exception error)
        {
            Notice = "导入失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
            ImportCommand.RaiseCanExecuteChanged();
        }
    }

    private void Replace(IReadOnlyList<HistoryEntry> entries, bool selectFirst)
    {
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }

        SelectedEntry = selectFirst ? Entries.FirstOrDefault() : null;
        Notice = null;
    }

    private void OpenFolder()
    {
        try
        {
            var directory = CsvFolder;
            Directory.CreateDirectory(directory);
            if (File.Exists(_store.FilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_store.FilePath}\"")
                {
                    UseShellExecute = true,
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{directory}\"")
                {
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception error)
        {
            Notice = "打开文件夹失败：" + error.Message;
        }
    }

    private void CopySelectedUrl()
    {
        Notice = LinkClipboard.CopyAndDescribe(SelectedEntry?.Url);
    }
}
