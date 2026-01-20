/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Futuristic Neon Palette
        'cyber': {
          50: '#f0f3ff',
          100: '#e0e7ff',
          200: '#c7d2fe',
          300: '#a5b4fc',
          400: '#818cf8',
          500: '#6366f1',
          600: '#4f46e5',
          700: '#4338ca',
          800: '#3730a3',
          900: '#312e81',
          950: '#1e1b4b',
        },
        'neon': {
          blue: '#00d4ff',
          purple: '#b24bf3',
          pink: '#ff2e97',
          cyan: '#00fff9',
          green: '#39ff14',
          yellow: '#fffc00',
        },
        'dark': {
          bg: '#0a0a0f',
          surface: '#13131a',
          elevated: '#1a1a24',
          border: '#2a2a3a',
        }
      },
      backgroundImage: {
        'gradient-cyber': 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        'gradient-neon': 'linear-gradient(135deg, #00d4ff 0%, #b24bf3 100%)',
        'gradient-dark': 'linear-gradient(180deg, #0a0a0f 0%, #1a1a24 100%)',
        'gradient-mesh': 'radial-gradient(at 40% 20%, #667eea 0px, transparent 50%), radial-gradient(at 80% 0%, #b24bf3 0px, transparent 50%), radial-gradient(at 0% 50%, #00d4ff 0px, transparent 50%)',
      },
      boxShadow: {
        'glow-blue': '0 0 20px rgba(0, 212, 255, 0.5), 0 0 40px rgba(0, 212, 255, 0.3)',
        'glow-purple': '0 0 20px rgba(178, 75, 243, 0.5), 0 0 40px rgba(178, 75, 243, 0.3)',
        'glow-pink': '0 0 20px rgba(255, 46, 151, 0.5), 0 0 40px rgba(255, 46, 151, 0.3)',
        'glow-cyan': '0 0 20px rgba(0, 255, 249, 0.5), 0 0 40px rgba(0, 255, 249, 0.3)',
        'glow-green': '0 0 20px rgba(57, 255, 20, 0.5), 0 0 40px rgba(57, 255, 20, 0.3)',
        'neon': '0 0 5px theme("colors.neon.blue"), 0 0 20px theme("colors.neon.blue")',
        'glass': '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
      },
      animation: {
        'float': 'float 6s ease-in-out infinite',
        'glow': 'glow 2s ease-in-out infinite alternate',
        'slide-up': 'slideUp 0.5s ease-out',
        'slide-down': 'slideDown 0.5s ease-out',
        'fade-in': 'fadeIn 0.6s ease-out',
        'scale-in': 'scaleIn 0.3s ease-out',
        'shimmer': 'shimmer 2s linear infinite',
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-20px)' },
        },
        glow: {
          '0%': { boxShadow: '0 0 5px rgba(0, 212, 255, 0.5), 0 0 20px rgba(0, 212, 255, 0.3)' },
          '100%': { boxShadow: '0 0 20px rgba(0, 212, 255, 0.8), 0 0 40px rgba(0, 212, 255, 0.5)' },
        },
        slideUp: {
          '0%': { transform: 'translateY(100px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        slideDown: {
          '0%': { transform: 'translateY(-100px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        scaleIn: {
          '0%': { transform: 'scale(0.9)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
        shimmer: {
          '0%': { backgroundPosition: '-1000px 0' },
          '100%': { backgroundPosition: '1000px 0' },
        },
      },
      backdropBlur: {
        xs: '2px',
      },
      fontFamily: {
        'display': ['Inter', 'system-ui', 'sans-serif'],
        'body': ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
