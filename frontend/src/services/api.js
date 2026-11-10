
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function getClasses() {
  const response = await fetch(`${API_BASE_URL}/classes`);

  if (!response.ok) {
    throw new Error('Failed to load admission classes.');
  }

  return await response.json();
}

export async function createHold(classId, registrationNumber) {
  const response = await fetch(
    `${API_BASE_URL}/classes/${classId}/holds`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Student-Id': registrationNumber,
      },
      body: JSON.stringify({
        admissionClassId: classId,
      }),
    }
  );

  const data = await response.json();

  if (!response.ok) {
    throw new Error(
      data.message || data.title || 'Unable to reserve this seat.'
    );
  }

  return data;
}



export async function confirmHold(holdId, registrationNumber) {
  const response = await fetch(
    `${API_BASE_URL}/holds/${holdId}/confirm`,
    {
      method: 'POST',
      headers: {
        'X-Student-Id': registrationNumber,
      },
    }
  );

  const data = await response.json();

  if (!response.ok) {
    throw new Error(
      data.message || data.title || 'Unable to confirm admission.'
    );
  }

  return data;
}

export async function cancelHold(holdId, registrationNumber) {
  const response = await fetch(
    `${API_BASE_URL}/holds/${holdId}`,
    {
      method: 'DELETE',
      headers: {
        'X-Student-Id': registrationNumber,
      },
    }
  );

  const data = await response.json();

  if (!response.ok) {
    throw new Error(
      data.message || data.title || 'Unable to cancel reservation.'
    );
  }

  return data;
}



export async function getMyHolds(registrationNumber) {
  const response = await fetch(`${API_BASE_URL}/me/holds`, {
    method: 'GET',
    headers: {
      'X-Student-Id': registrationNumber,
    },
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(
      data.message || data.detail || data.title ||
      'Unable to load your applications.'
    );
  }

  return data;
}

