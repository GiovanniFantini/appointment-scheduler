import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import { createProxyMiddleware } from 'http-proxy-middleware';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = process.env.PORT || 8080;
const API_URL = process.env.API_URL || 'https://appointment-scheduler-api.azurewebsites.net';

// Health check endpoints for Azure App Service
app.get('/health', (req, res) => {
  res.status(200).json({ status: 'healthy', app: 'admin-app' });
});

app.get('/health/ready', (req, res) => {
  res.status(200).json({ status: 'ready', app: 'admin-app' });
});

app.get('/health/live', (req, res) => {
  res.status(200).json({ status: 'live', app: 'admin-app' });
});

// Proxy API requests to backend
app.use('/api', createProxyMiddleware({
  target: API_URL,
  changeOrigin: true,
  onProxyReq: (proxyReq, req, res) => {
    console.log(`[Proxy] ${req.method} ${req.path} -> ${API_URL}${req.path}`);
  },
  onError: (err, req, res) => {
    console.error('[Proxy Error]', err.message);
    res.status(500).json({ error: 'Proxy error', message: err.message });
  }
}));

// Serve static files from the dist directory
app.use(express.static(path.join(__dirname, 'dist')));

// Handle client-side routing - send all requests to index.html
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'dist', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`Admin app is running on port ${PORT}`);
});
