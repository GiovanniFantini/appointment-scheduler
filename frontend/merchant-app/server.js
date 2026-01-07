import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = process.env.PORT || 8080;

// Health check endpoints for Azure App Service
app.get('/health', (req, res) => {
  res.status(200).json({ status: 'healthy', app: 'merchant-app' });
});

app.get('/health/ready', (req, res) => {
  res.status(200).json({ status: 'ready', app: 'merchant-app' });
});

app.get('/health/live', (req, res) => {
  res.status(200).json({ status: 'live', app: 'merchant-app' });
});

// Serve static files from the dist directory
app.use(express.static(path.join(__dirname, 'dist')));

// Handle client-side routing - send all requests to index.html
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'dist', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`Merchant app is running on port ${PORT}`);
});
