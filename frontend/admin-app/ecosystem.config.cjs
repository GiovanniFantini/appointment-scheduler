module.exports = {
  apps: [{
    name: 'admin-app',
    script: 'server.js',
    instances: 1,
    exec_mode: 'cluster',
    env: {
      NODE_ENV: 'production',
      PORT: process.env.PORT || 8080
    }
  }]
};
