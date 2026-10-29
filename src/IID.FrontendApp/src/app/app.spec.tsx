import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { describe, expect, it } from 'vitest';
import { AppComponent } from './app';
import { store } from './core/store/store';

describe('AppComponent', () => {
  it('should create the app', () => {
    const { container } = render(
      <Provider store={store}>
        <AppComponent />
      </Provider>
    );
    expect(container.querySelector('.iid-app')).toBeInTheDocument();
  });
});
