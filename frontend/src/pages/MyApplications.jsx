
import { useState } from 'react';
import Navbar from '../components/Navbar';
import { getMyHolds } from '../services/api';

function MyApplications() {
  const [registrationNumber, setRegistrationNumber] = useState('');
  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [searched, setSearched] = useState(false);

  const handleSearch = async (event) => {
    event.preventDefault();

    if (!/^\d{17}$/.test(registrationNumber)) {
      setError('Please enter a valid 17-digit registration number.');
      return;
    }

    try {
      setLoading(true);
      setError('');
      setSearched(false);

      const data = await getMyHolds(registrationNumber);

      setApplications(data);
      setSearched(true);
    } catch (err) {
      setError(err.message);
      setApplications([]);
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'bg-primary';

      case 'confirmed':
        return 'bg-success';

      case 'cancelled':
        return 'bg-secondary';

      case 'expired':
        return 'bg-danger';

      default:
        return 'bg-dark';
    }
  };

  return (
    <>
      <Navbar />

      <div className="container py-5">
        <div className="text-center mb-4">
          <h1 className="fw-bold">My Applications</h1>
          <p className="text-muted">
            View and manage your school admission applications.
          </p>
        </div>

        <div className="card shadow-sm mb-4">
          <div className="card-body p-4">
            <h5 className="mb-3">
              <i className="bi bi-person-vcard me-2"></i>
              Student Identification
            </h5>

            <form onSubmit={handleSearch}>
              <label className="form-label">
                Student Registration Number
              </label>

              <input
                type="text"
                className="form-control mb-3"
                placeholder="Enter your 17-digit registration number"
                value={registrationNumber}
                onChange={(event) => {
                  setRegistrationNumber(
                    event.target.value.replace(/\D/g, '').slice(0, 17)
                  );
                }}
              />

              <button
                type="submit"
                className="btn btn-primary"
                disabled={loading}
              >
                {loading ? 'Searching...' : 'View Applications'}
              </button>
            </form>
          </div>
        </div>

        {error && (
          <div className="alert alert-danger">
            {error}
          </div>
        )}

        {searched && applications.length === 0 && (
          <div className="alert alert-info">
            No admission applications found for this registration number.
          </div>
        )}

        {applications.length > 0 && (
          <>
            <h4 className="mb-3">
              Your Applications ({applications.length})
            </h4>

            <div className="row g-3">
              {applications.map((application) => (
                <div className="col-md-6" key={application.id}>
                  <div className="card shadow-sm h-100">
                    <div className="card-body">
                      <div className="d-flex justify-content-between align-items-start mb-3">
                        <h5 className="card-title">
                          {application.className}
                        </h5>

                        <span
                          className={`badge ${getStatusBadge(application.status)}`}
                        >
                          {application.status}
                        </span>
                      </div>

                      <p className="mb-2">
                        <strong>Application ID:</strong>
                        <br />
                        <small>{application.id}</small>
                      </p>

                      <p className="mb-2">
                        <strong>Created:</strong>{' '}
                        {new Date(application.createdAt).toLocaleString()}
                      </p>

                      {application.expiresAt && (
                        <p className="mb-2">
                          <strong>Expires:</strong>{' '}
                          {new Date(application.expiresAt).toLocaleString()}
                        </p>
                      )}

                      {application.confirmedAt && (
                        <p className="mb-2">
                          <strong>Confirmed:</strong>{' '}
                          {new Date(application.confirmedAt).toLocaleString()}
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </>
  );
}

export default MyApplications;
