const API_BASE_URL = 'http://localhost:5133/api';

/**
 * Fetches all stock exchanges from the backend API
 * Backend returns: { exchangeId, name, country, currency }
 * @returns {Promise<Array>} Array of stock exchange objects
 */
export async function fetchStockExchanges() {
  try {
    const token = localStorage.getItem('token');
    const response = await fetch(`${API_BASE_URL}/stockexchanges`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token && { 'Authorization': `Bearer ${token}` })
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    // Transform backend response to match frontend display expectations
    // Backend: { exchangeId, name, country, currency }
    // Frontend: { id, name, code, location, description }
    return data.map(exchange => ({
      id: exchange.exchangeId,
      name: exchange.name,
      code: exchange.currency, // Using currency as display code
      location: exchange.country,
      description: `Stock exchange in ${exchange.country} trading in ${exchange.currency}`,
      // Keep original backend data
      exchangeId: exchange.exchangeId,
      country: exchange.country,
      currency: exchange.currency
    }));
  } catch (error) {
    console.error('Error fetching stock exchanges:', error);
    throw error;
  }
}
