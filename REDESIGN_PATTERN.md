# Futuristic Redesign Pattern

## Colors & Themes
- Background: `bg-gradient-dark` (already in body)
- Cards: `glass-card` or `glass-card-dark`
- Borders: `border-white/10`, `border-neon-*/30`
- Text: `text-white`, `text-gray-300`, `text-gray-400`
- Gradients: `gradient-text` for titles

## Components

### Headers
```tsx
<header className="glass-card border-b border-white/10 sticky top-0 z-40 backdrop-blur-xl">
  <div className="container mx-auto px-4 py-4">
    <div className="flex justify-between items-center">
      <h1 className="text-2xl font-bold gradient-text flex items-center gap-2">
        {/* Icon SVG */}
        Title
      </h1>
      <div className="flex items-center gap-3">
        {/* Buttons */}
      </div>
    </div>
  </div>
</header>
```

### Buttons
- Primary: `bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105`
- Secondary: `glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 border border-white/10`
- Danger: `glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 text-gray-300 hover:text-red-400 border border-white/10`

### Cards
```tsx
<div className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-blue/50 transition-all hover:shadow-glow-blue animate-slide-up">
  {/* Content */}
</div>
```

### Inputs
```tsx
<input className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all duration-300" />
```

### Status Badges
- Pending: `bg-yellow-500/20 text-yellow-400 border-yellow-500/30`
- Confirmed: `bg-neon-green/20 text-neon-green border-neon-green/30`
- Rejected: `bg-red-500/20 text-red-400 border-red-500/30`
- Completed: `bg-neon-blue/20 text-neon-blue border-neon-blue/30`

## Animations
- Appear: `animate-fade-in`, `animate-scale-in`, `animate-slide-up`
- Hover: `transform hover:scale-105`, `hover:shadow-glow-*`
- Loading: spinner with `animate-spin`
