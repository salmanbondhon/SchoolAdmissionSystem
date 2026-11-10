import { Link } from 'react-router-dom';


function Navbar() {
  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
      <div className="container">
        <a className="navbar-brand fw-bold" href="/">
          <i className="bi bi-mortarboard-fill me-2"></i>
          School Admission System
        </a>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarContent"
        >
          <span className="navbar-toggler-icon"></span>
        </button>

        <div
          className="collapse navbar-collapse"
          id="navbarContent"
        >
          <ul className="navbar-nav ms-auto">
  <li className="nav-item">
    <Link className="nav-link" to="/">
      Home
    </Link>
  </li>

  <li className="nav-item">
    <Link className="nav-link" to="/#classes">
      Admission Classes
    </Link>
  </li>

  <li className="nav-item">
    <Link className="nav-link" to="/my-applications">
      My Applications
    </Link>
  </li>
</ul>
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
