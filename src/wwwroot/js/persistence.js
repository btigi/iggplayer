const PLAYLIST_KEY = 'iggplayer-playlist';
const SEARCH_KEY = 'iggplayer-last-search';
const ALBUM_FOLDER_KEY = 'iggplayer-last-album-folder';

export function getPlaylistState() {
	try {
		const raw = localStorage.getItem(PLAYLIST_KEY);
		if (!raw)
			return null;

		const o = JSON.parse(raw);
		if (!o || !Array.isArray(o.trackIds) || o.trackIds.length === 0)
			return null;
		return {
			TrackIds: o.trackIds, CurrentIndex: o.currentIndex ?? 0
		};
	} catch {
		return null;
	}
}

export function setPlaylistState(state) {
	try {
		const ids = state?.trackIds ?? state?.TrackIds;
		if (state && ids && ids.length > 0) {
			const toStore = { trackIds: ids, currentIndex: state.currentIndex ?? state.CurrentIndex ?? 0 };
			localStorage.setItem(PLAYLIST_KEY, JSON.stringify(toStore));
		} else {
			localStorage.removeItem(PLAYLIST_KEY);
		}
	} catch (_) { }
}

export function getLastSearch() {
	try {
		return localStorage.getItem(SEARCH_KEY) || '';
	} catch {
		return '';
	}
}

export function setLastSearch(query) {
	try {
		localStorage.removeItem(ALBUM_FOLDER_KEY);
		if (query) {
			localStorage.setItem(SEARCH_KEY, query);
		} else {
			localStorage.removeItem(SEARCH_KEY);
		}
	} catch (_) { }
}

export function getLastAlbumFolder() {
	try {
		return localStorage.getItem(ALBUM_FOLDER_KEY) || '';
	} catch {
		return '';
	}
}

export function setLastAlbumFolder(folder) {
	try {
		if (folder) {
			localStorage.setItem(ALBUM_FOLDER_KEY, folder);
		} else {
			localStorage.removeItem(ALBUM_FOLDER_KEY);
		}
	} catch (_) { }
}
