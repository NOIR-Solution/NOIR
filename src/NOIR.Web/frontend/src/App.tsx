import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/contexts/AuthContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { PortalLayout } from '@/layouts/PortalLayout'
import LoginPage from '@/pages/Login'
import LandingPage from '@/pages/Landing'
import Dashboard from '@/pages/portal/Dashboard'
import ForgotPasswordPage from '@/pages/forgot-password/ForgotPassword'
import VerifyOtpPage from '@/pages/forgot-password/VerifyOtp'
import ResetPasswordPage from '@/pages/forgot-password/ResetPassword'
import SuccessPage from '@/pages/forgot-password/Success'
import './index.css'

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Toaster
          position="top-center"
          richColors
          toastOptions={{
            classNames: {
              success: 'bg-green-50 border-green-200 text-green-800',
              error: 'bg-red-50 border-red-200 text-red-800',
              info: 'bg-blue-50 border-blue-200 text-blue-800',
            }
          }}
        />
        <Routes>
          {/* Public Routes */}
          <Route path="/" element={<LandingPage />} />
          <Route path="/login" element={<LoginPage />} />

          {/* Forgot Password Flow */}
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/forgot-password/verify" element={<VerifyOtpPage />} />
          <Route path="/forgot-password/reset" element={<ResetPasswordPage />} />
          <Route path="/forgot-password/success" element={<SuccessPage />} />

          {/* Protected Portal Routes */}
          <Route
            path="/portal"
            element={
              <ProtectedRoute>
                <PortalLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Dashboard />} />
          </Route>

          {/* Catch-all redirect to landing */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

export default App
