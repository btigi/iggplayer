using IggPlayer.Data;

namespace IggPlayer.Services;

public class PlaylistService
{
    private readonly List<Track> _queue = [];
    private int _currentIndex = -1;

    public event Action? OnChanged;

    public IReadOnlyList<Track> Queue => _queue;
    public int CurrentIndex => _currentIndex;
    public Track? CurrentTrack => _currentIndex >= 0 && _currentIndex < _queue.Count ? _queue[_currentIndex] : null;

    public void Enqueue(Track track)
    {
        _queue.Add(track);
        if (_queue.Count == 1)
            _currentIndex = 0;
        NotifyChanged();
    }

    public void PlayNow(Track track)
    {
        _queue.Clear();
        _queue.Add(track);
        _currentIndex = 0;
        NotifyChanged();
    }

    public void JumpTo(int index)
    {
        if (index >= 0 && index < _queue.Count)
        {
            _currentIndex = index;
            NotifyChanged();
        }
    }

    public Track? Next()
    {
        if (_currentIndex + 1 < _queue.Count)
        {
            _currentIndex++;
            NotifyChanged();
            return CurrentTrack;
        }
        return null;
    }

    public Track? Previous()
    {
        if (_currentIndex - 1 >= 0)
        {
            _currentIndex--;
            NotifyChanged();
            return CurrentTrack;
        }
        return null;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _queue.Count) return;

        _queue.RemoveAt(index);

        if (_queue.Count == 0)
        {
            _currentIndex = -1;
        }
        else if (index < _currentIndex)
        {
            _currentIndex--;
        }
        else if (index == _currentIndex)
        {
            if (_currentIndex >= _queue.Count)
                _currentIndex = _queue.Count - 1;
        }

        NotifyChanged();
    }

    public void Clear()
    {
        _queue.Clear();
        _currentIndex = -1;
        NotifyChanged();
    }

    private void NotifyChanged() => OnChanged?.Invoke();
}