import { useState, useEffect, createContext, useContext, ReactNode } from 'react';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

interface Toast {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastContextType {
  showToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const useToast = () => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within ToastProvider');
  }
  return context;
};

interface ToastProviderProps {
  children: ReactNode;
}

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = (message: string, type: ToastType = 'info') => {
    const id = Math.random().toString(36).substring(7);
    setToasts(prev => [...prev, { id, message, type }]);

    setTimeout(() => {
      setToasts(prev => prev.filter(toast => toast.id !== id));
    }, 4000);
  };

  const removeToast = (id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id));
  };

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="fixed top-4 right-4 z-[9999] space-y-2">
        {toasts.map(toast => (
          <ToastItem key={toast.id} toast={toast} onClose={() => removeToast(toast.id)} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

interface ToastItemProps {
  toast: Toast;
  onClose: () => void;
}

function ToastItem({ toast, onClose }: ToastItemProps) {
  useEffect(() => {
    const timer = setTimeout(onClose, 4000);
    return () => clearTimeout(timer);
  }, [onClose]);

  const getToastStyles = () => {
    switch (toast.type) {
      case 'success':
        return 'bg-gradient-to-r from-neon-green/90 to-neon-cyan/90 border-neon-green/50';
      case 'error':
        return 'bg-gradient-to-r from-red-500/90 to-neon-pink/90 border-red-500/50';
      case 'warning':
        return 'bg-gradient-to-r from-yellow-500/90 to-orange-500/90 border-yellow-500/50';
      case 'info':
        return 'bg-gradient-to-r from-neon-blue/90 to-neon-cyan/90 border-neon-blue/50';
    }
  };

  const getIcon = () => {
    switch (toast.type) {
      case 'success':
        return '✓';
      case 'error':
        return '✕';
      case 'warning':
        return '⚠';
      case 'info':
        return 'ℹ';
    }
  };

  return (
    <div
      className={`${getToastStyles()} backdrop-blur-lg text-white px-6 py-4 rounded-2xl shadow-glow-cyan border-2 min-w-[300px] max-w-md animate-slide-up`}
    >
      <div className="flex items-center gap-3">
        <div className="text-2xl font-bold">{getIcon()}</div>
        <div className="flex-1 font-semibold">{toast.message}</div>
        <button
          onClick={onClose}
          className="text-white/80 hover:text-white transition-colors text-xl font-bold"
        >
          ×
        </button>
      </div>
    </div>
  );
}
