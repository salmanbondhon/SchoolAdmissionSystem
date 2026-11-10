

import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from './pages/Home';
import MyApplications from './pages/MyApplications';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/my-applications" element={<MyApplications />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
