/** @type {import('tailwindcss').Config} */
module.exports = {
  presets: [require('../shared-ui/tailwind.preset.cjs')],
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
    '../shared-ui/src/**/*.{js,ts,jsx,tsx}'
  ],
  plugins: []
}
