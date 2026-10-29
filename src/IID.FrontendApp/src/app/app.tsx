import { RouterProvider } from 'react-router-dom';
import { router } from './app.routes';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container';
import './app.scss';

export function AppComponent() {
  return (
    <div className="iid-app">
      <RouterProvider router={router} />
      <ToastContainerComponent />
    </div>
  );
}
