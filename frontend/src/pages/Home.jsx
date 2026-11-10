import { useEffect, useState } from "react";
import Navbar from "../components/Navbar";
import {
  getClasses,
  createHold,
  confirmHold,
  cancelHold,
} from "../services/api";

function Home() {
  const [classes, setClasses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [registrationNumber, setRegistrationNumber] = useState("");
  const [holdMessage, setHoldMessage] = useState("");
  const [reservingClassId, setReservingClassId] = useState(null);
  const [activeHold, setActiveHold] = useState(null);
  const [currentTime, setCurrentTime] = useState(0);

  async function handleReserve(classId) {
    if (!/^\d{17}$/.test(registrationNumber)) {
      setHoldMessage("Please enter a valid 17-digit registration number.");
      return;
    }

    try {
      setReservingClassId(classId);
      setHoldMessage("");

      const result = await createHold(classId, registrationNumber);

      setActiveHold(result);

      setHoldMessage("Your seat has been temporarily reserved successfully!");

      await getClasses().then(setClasses);
    } catch (err) {
      setHoldMessage(err.message);
    } finally {
      setReservingClassId(null);
    }
  }


  const handleConfirm = async (holdId) => {
  try {
    setHoldMessage('');
    setReservingClassId(holdId);

    const result = await confirmHold(holdId, registrationNumber);

    setActiveHold((previous) => ({
      ...previous,
      status: result.status,
      confirmedAt: result.confirmedAt,
    }));

    setHoldMessage(
      `Admission confirmed successfully! Transaction: ${result.transactionReference}`
    );

    const updatedClasses = await getClasses();
    setClasses(updatedClasses);
  } catch (error) {
    setHoldMessage(error.message);
  } finally {
    setReservingClassId(null);
  }
};

const handleCancel = async (holdId) => {
  try {
    setHoldMessage('');
    setReservingClassId(holdId);

    await cancelHold(holdId, registrationNumber);

    setActiveHold(null);
    setHoldMessage('Your reservation has been cancelled.');

    const updatedClasses = await getClasses();
    setClasses(updatedClasses);
  } catch (error) {
    setHoldMessage(error.message);
  } finally {
    setReservingClassId(null);
  }
};

  useEffect(() => {
    let isMounted = true;

    const fetchClasses = () => {
      getClasses()
        .then((data) => {
          if (isMounted) {
            setClasses(data);
            setError("");
          }
        })
        .catch((err) => {
          if (isMounted) {
            setError(err.message);
          }
        })
        .finally(() => {
          if (isMounted) {
            setLoading(false);
          }
        });
    };

    fetchClasses();

    const interval = setInterval(fetchClasses, 5000);

    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, []);

  useEffect(() => {
    const updateTime = () => {
      setCurrentTime(Date.now());
    };

    updateTime();

    const timer = setInterval(updateTime, 1000);

    return () => clearInterval(timer);
  }, []);

  const remainingSeconds = activeHold
    ? Math.max(
        0,
        Math.ceil(
          (new Date(activeHold.expiresAt).getTime() - currentTime) / 1000,
        ),
      )
    : 0;

  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;

  const countdown = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;

  return (
    <>
      <Navbar />

      <main className="container py-5">
        <section className="text-center mb-5">
          <h1 className="fw-bold">School Admission Portal</h1>
          <p className="text-secondary">
            Find available admission classes and reserve your seat online.
          </p>
          <span className="badge bg-success fs-6">Admission Open</span>
        </section>

        <section className="card shadow-sm border-0 mb-5">
          <div className="card-body p-4">
            <h4 className="fw-bold mb-3">
              <i className="bi bi-person-vcard me-2"></i>
              Student Identification
            </h4>

            <label className="form-label">Student Registration Number</label>
            <input
              type="text"
              className="form-control"
              placeholder="Enter your 17-digit registration number"
              value={registrationNumber}
              onChange={(e) => {
                const value = e.target.value;

                if (/^\d{0,17}$/.test(value)) {
                  setRegistrationNumber(value);
                }
              }}
              maxLength={17}
            />

            <small className="text-secondary">
              Enter your 17-digit registration number to manage your admission
              applications.
            </small>

            {registrationNumber.length > 0 &&
              registrationNumber.length !== 17 && (
                <div className="text-danger small mt-1">
                  Registration number must contain exactly 17 digits.
                </div>
              )}

            {registrationNumber.length === 17 && (
              <div className="text-success small mt-1">
                <i className="bi bi-check-circle me-1"></i>
                Registration number is valid.
              </div>
            )}
          </div>
        </section>

        <section id="classes">
          <h3 className="fw-bold mb-4">Available Admission Classes</h3>

          
{activeHold && (
  <div className="card border-success shadow-sm mb-4">
    <div className="card-body p-4">
      <h4 className="text-success">
        <i className="bi bi-check-circle-fill me-2"></i>
        Your Reservation
      </h4>

      <p className="mb-2">
        <strong>Class:</strong> {activeHold.className}
      </p>

      <p className="mb-2">
        <strong>Hold ID:</strong> {activeHold.id}
      </p>

      <p className="mb-2">
        <strong>Status:</strong> {activeHold.status}
      </p>

      {activeHold.status === 'Active' && (
        <div className="mt-3">
          <p className="mb-1">Time remaining:</p>

          <h2
            className={`fw-bold ${
              remainingSeconds <= 60
                ? 'text-danger'
                : 'text-success'
            }`}
          >
            {countdown}
          </h2>

          {remainingSeconds === 0 ? (
            <div className="alert alert-danger">
              Your reservation time has expired.
            </div>
          ) : (
            <div className="alert alert-success">
              Your seat is temporarily reserved.
            </div>
          )}

          <div className="d-flex gap-2 flex-wrap mt-3">
            <button
              className="btn btn-primary"
              onClick={() => handleConfirm(activeHold.id)}
              disabled={
                remainingSeconds === 0 ||
                reservingClassId !== null
              }
            >
              {reservingClassId === activeHold.id
                ? 'Processing...'
                : 'Confirm Admission'}
            </button>

            <button
              className="btn btn-outline-danger"
              onClick={() => handleCancel(activeHold.id)}
              disabled={reservingClassId !== null}
            >
              Cancel Reservation
            </button>
          </div>
        </div>
      )}

      {activeHold.status === 'Confirmed' && (
        <div className="alert alert-success mt-3 mb-0">
          <i className="bi bi-check-circle-fill me-2"></i>
          Your admission has been confirmed!
        </div>
      )}
    </div>
  </div>
)}


          {holdMessage && (
            <div className="alert alert-info mt-3">{holdMessage}</div>
          )}

          {loading && (
            <div className="text-center py-4">
              <div className="spinner-border text-primary"></div>
              <p className="mt-2">Loading admission classes...</p>
            </div>
          )}

          {error && <div className="alert alert-danger">{error}</div>}

          {!loading && !error && classes.length === 0 && (
            <div className="alert alert-info">
              No admission classes available.
            </div>
          )}

          <div className="row g-4">
            {classes.map((item) => (
              <div className="col-md-6 col-lg-4" key={item.id}>
                <div className="card h-100 shadow-sm border-0">
                  <div className="card-body">
                    <h5 className="fw-bold mb-3">
                      <i className="bi bi-mortarboard-fill text-primary me-2"></i>
                      {item.name}
                    </h5>

                    <p>Total Seats: {item.totalSeats}</p>
                    <p>Confirmed Seats: {item.confirmedSeats}</p>
                    <p>Held Seats: {item.heldSeats}</p>

                    <div className="d-flex justify-content-between">
                      <span>Available Seats</span>
                      <strong className="text-success">
                        {item.availableSeats}
                      </strong>
                    </div>

                    <div className="progress mt-2">
                      <div
                        className="progress-bar bg-success"
                        role="progressbar"
                        style={{
                          width: `${
                            item.totalSeats > 0
                              ? (item.availableSeats / item.totalSeats) * 100
                              : 0
                          }%`,
                        }}
                      ></div>
                    </div>

                    <span
                      className={`badge mt-3 ${
                        item.availableSeats > 0 ? "bg-success" : "bg-danger"
                      }`}
                    >
                      {item.availableSeats > 0
                        ? "Seats Available"
                        : "Fully Booked"}
                    </span>

                    <button
                      className="btn btn-primary w-100 mt-3"
                      disabled={
                        !/^\d{17}$/.test(registrationNumber) ||
                        item.availableSeats <= 0 ||
                        reservingClassId !== null
                      }
                      onClick={() => handleReserve(item.id)}
                    >
                      {reservingClassId === item.id
                        ? "Reserving..."
                        : "Reserve Seat"}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>
      </main>
    </>
  );
}

export default Home;

