
import { useState, useEffect, useCallback } from 'react';
import Navbar from '../components/Navbar';
import { getMyHolds } from '../services/api';

function MyApplications() {
  const [registrationNumber, setRegistrationNumber] = useState(() => {
    const saved = localStorage.getItem('studentRegistrationNumber');
    return saved && /^\d{17}$/.test(saved) ? saved : '';
  });

  const [activeRegistrationNumber, setActiveRegistrationNumber] =
    useState(() => {
      const saved = localStorage.getItem('studentRegistrationNumber');
      return saved && /^\d{17}$/.test(saved) ? saved : '';
    });

  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [searched, setSearched] = useState(false);
  const [lastUpdated, setLastUpdated] = useState(null);

  // Initial loading and student restoration
  useEffect(() => {
    if (!/^\d{17}$/.test(activeRegistrationNumber)) {
      return;
    }

    let ignore = false;

    const fetchApplications = async () => {
      try {
        const data = await getMyHolds(activeRegistrationNumber);

        if (!ignore) {
          setApplications(data);
          setError('');
          setSearched(true);
          setLastUpdated(new Date());
        }
      } catch (err) {
        if (!ignore) {
          setError(err.message);
        }
      }
    };

    fetchApplications();

    return () => {
      ignore = true;
    };
  }, [activeRegistrationNumber]);

  // Fetch applications for manual search and periodic refresh
  const loadApplications = useCallback(async (studentId) => {
    try {
      const data = await getMyHolds(studentId);

      setApplications(data);
      setError('');
      setSearched(true);
      setLastUpdated(new Date());
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Automatically refresh applications every 5 seconds
  useEffect(() => {
    if (!/^\d{17}$/.test(activeRegistrationNumber)) {
      return;
    }

    const intervalId = setInterval(() => {
      loadApplications(activeRegistrationNumber);
    }, 5000);

    return () => clearInterval(intervalId);
  }, [activeRegistrationNumber, loadApplications]);

  // Manually search applications
  const handleSearch = async (event) => {
    event.preventDefault();

    if (!/^\d{17}$/.test(registrationNumber)) {
      setError('Please enter a valid 17-digit registration number.');
      return;
    }

    localStorage.setItem(
      'studentRegistrationNumber',
      registrationNumber
    );

    setError('');
    setLoading(true);

    if (activeRegistrationNumber === registrationNumber) {
      await loadApplications(registrationNumber);
    } else {
      setActiveRegistrationNumber(registrationNumber);
    }
  };

  // Clear saved registration number
  const handleClear = () => {
    localStorage.removeItem('studentRegistrationNumber');

    setRegistrationNumber('');
    setActiveRegistrationNumber('');
    setApplications([]);
    setSearched(false);
    setError('');
    setLastUpdated(null);
    setLoading(false);
  };

  // Application status badge
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

        {/* Student Identification */}
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

              <button
                type="button"
                className="btn btn-outline-secondary ms-2"
                onClick={handleClear}
              >
                Clear Saved Number
              </button>
            </form>
          </div>
        </div>

        {/* Loading */}
        {loading && (
          <div className="text-center my-4">
            <div className="spinner-border text-primary" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <p className="text-muted mt-2">
              Loading your applications...
            </p>
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="alert alert-danger">{error}</div>
        )}

        {/* Last Updated */}
        {searched && lastUpdated && (
          <p className="text-muted small">
            <i className="bi bi-arrow-repeat me-1"></i>
            Automatically refreshing every 5 seconds.
            Last updated: {lastUpdated.toLocaleTimeString()}
          </p>
        )}

        {/* No Applications */}
        {searched && applications.length === 0 && !loading && (
          <div className="alert alert-info">
            No admission applications found for this registration number.
          </div>
        )}

        {/* Application List */}
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

                      {/* Payment Information */}
                      {application.transactionReference && (
                        <div className="border-top pt-3 mt-3">
                          <h6 className="text-success">
                            <i className="bi bi-credit-card me-2"></i>
                            Payment Information
                          </h6>

                          <p className="mb-2">
                            <strong>Transaction Reference:</strong>
                            <br />
                            <small>
                              {application.transactionReference}
                            </small>
                          </p>

                          <p className="mb-2">
                            <strong>Payment Amount:</strong>{' '}
                            {application.paymentAmount}
                          </p>

                          <p className="mb-2">
                            <strong>Payment Status:</strong>{' '}
                            <span
                              className={`badge ${
                                application.paymentSuccessful
                                  ? 'bg-success'
                                  : 'bg-danger'
                              }`}
                            >
                              {application.paymentSuccessful
                                ? 'Successful'
                                : 'Failed'}
                            </span>
                          </p>

                          {application.paidAt && (
                            <p className="mb-0">
                              <strong>Paid At:</strong>{' '}
                              {new Date(application.paidAt).toLocaleString()}
                            </p>
                          )}
                        </div>
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
