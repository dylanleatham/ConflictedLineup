export function UploadPage() {
  return (
    <div className="upload-page">
      <div className="upload-container">
        <h1 className="upload-title">Create Your Festival Playlist</h1>
        <p className="upload-subtitle">Upload a poster or search for a festival</p>

        <div className="upload-options">
          <div className="upload-card">
            <div className="card-icon upload-icon">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
                <path
                  d="M12 16L12 8M12 8L9 11M12 8L15 11"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M3 15V18C3 19.1046 3.89543 20 5 20H19C20.1046 20 21 19.1046 21 18V15"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                />
              </svg>
            </div>
            <h3 className="card-title">Upload Poster</h3>
            <p className="card-description">Drag and drop or click to upload</p>
          </div>

          <div className="upload-card">
            <div className="card-icon search-icon">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
                <circle
                  cx="11"
                  cy="11"
                  r="7"
                  stroke="currentColor"
                  strokeWidth="2"
                />
                <path
                  d="M20 20L16 16"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                />
              </svg>
            </div>
            <h3 className="card-title">Search Festival</h3>
            <p className="card-description">Search by festival name</p>
          </div>
        </div>
      </div>
    </div>
  );
}
