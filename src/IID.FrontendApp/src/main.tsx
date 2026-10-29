import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { AppComponent } from './app/app';
import { setupAuthInterceptor } from './app/core/interceptors/auth.interceptor';
import { store } from './app/core/store/store';
import './app/core/services/theme.service';
import './styles.scss';

setupAuthInterceptor();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Provider store={store}>
      <AppComponent />
    </Provider>
  </StrictMode>
);
