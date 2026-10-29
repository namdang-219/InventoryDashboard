import { FormEvent, useEffect, useState } from 'react';
import {
  CreateDealershipRequest,
  Dealership,
  UpdateDealershipRequest
} from '../../../../core/models/dealership.model';
import { dealershipService } from '../../../../core/services/dealership.service';
import { toastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import './dealership-form-modal.scss';

export interface DealershipFormModalProps {
  dealership?: Dealership | null;
  onClose: () => void;
  onSaved?: () => void;
}

export function DealershipFormModalComponent({
  dealership = null,
  onClose,
  onSaved
}: DealershipFormModalProps) {
  const [name, setName] = useState(dealership?.name ?? '');
  const [code, setCode] = useState(dealership?.code ?? '');
  const [city, setCity] = useState(dealership?.city ?? '');
  const [state, setState] = useState(dealership?.state ?? '');
  const [phone, setPhone] = useState(dealership?.phone ?? '');

  const [touched, setTouched] = useState({
    name: false,
    code: false,
    city: false,
    state: false,
    phone: false
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (dealership) {
      setName(dealership.name);
      setCode(dealership.code);
      setCity(dealership.city);
      setState(dealership.state);
      setPhone(dealership.phone ?? '');
    } else {
      setName('');
      setCode('');
      setCity('');
      setState('');
      setPhone('');
    }
  }, [dealership]);

  const isEditMode = !!dealership;

  const isNameInvalid = name.length === 0 || name.length > 150;
  const isCodeInvalid = code.length === 0 || code.length > 20;
  const isCityInvalid = city.length === 0 || city.length > 100;
  const isStateInvalid = state.length === 0 || state.length > 50;
  const isPhoneInvalid = phone.length > 50;
  const isFormInvalid =
    isNameInvalid || isCodeInvalid || isCityInvalid || isStateInvalid || isPhoneInvalid;

  const onSubmit = async (e: FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    if (isFormInvalid) {
      setTouched({ name: true, code: true, city: true, state: true, phone: true });
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    if (dealership) {
      const req: UpdateDealershipRequest = {
        name: name.trim(),
        code: code.trim().toUpperCase(),
        city: city.trim(),
        state: state.trim().toUpperCase(),
        phone: phone.trim()
      };

      try {
        await dealershipService.updateDealership(dealership.id, req);
        setIsSubmitting(false);
        toastService.success(
          'Dealership Updated',
          `Showroom "${req.name}" details have been updated successfully.`
        );
        onSaved?.();
        onClose();
      } catch (err) {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to update dealership.');
        setErrorMessage(msg);
        toastService.error('Update Failed', msg);
      }
    } else {
      const req: CreateDealershipRequest = {
        name: name.trim(),
        code: code.trim().toUpperCase(),
        city: city.trim(),
        state: state.trim().toUpperCase(),
        phone: phone.trim()
      };

      try {
        await dealershipService.createDealership(req);
        setIsSubmitting(false);
        toastService.success(
          'Dealership Registered',
          `Showroom "${req.name}" (${req.code}) has been created.`
        );
        onSaved?.();
        onClose();
      } catch (err) {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to create dealership.');
        setErrorMessage(msg);
        toastService.error('Registration Failed', msg);
      }
    }
  };

  return (
    <div className="iid-dealership-form-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">
              {isEditMode ? 'edit_location_alt' : 'add_business'}
            </span>
            <h3>{isEditMode ? 'Edit Dealership Showroom' : 'Register New Dealership'}</h3>
          </div>
          <button type="button" className="close-btn" onClick={onClose} title="Close dialog">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {errorMessage && (
          <div className="error-banner">
            <span className="material-symbols-outlined">error</span>
            <span>{errorMessage}</span>
          </div>
        )}

        <form onSubmit={onSubmit} className="modal-form" noValidate>
          {/* Showroom Name */}
          <div className="form-group">
            <label className="iid-form-label" htmlFor="dealerName">
              Showroom / Dealership Name *
            </label>
            <input
              id="dealerName"
              type="text"
              className="iid-input"
              placeholder="E.g., Apex Motors - Downtown"
              value={name}
              onChange={e => setName(e.target.value)}
              onBlur={() => setTouched(prev => ({ ...prev, name: true }))}
            />
            {touched.name && isNameInvalid && (
              <span className="error-text">Showroom name is required (max 150 characters).</span>
            )}
          </div>

          {/* Code & Phone Row */}
          <div className="form-row two-col">
            <div className="form-group">
              <label className="iid-form-label" htmlFor="dealerCode">
                Showroom Code *
              </label>
              <input
                id="dealerCode"
                type="text"
                className="iid-input font-mono uppercase"
                placeholder="E.g., LA-01, DAL-02"
                value={code}
                onChange={e => setCode(e.target.value)}
                onBlur={() => setTouched(prev => ({ ...prev, code: true }))}
              />
              {touched.code && isCodeInvalid && (
                <span className="error-text">Unique code is required (e.g. DAL-01).</span>
              )}
            </div>

            <div className="form-group">
              <label className="iid-form-label" htmlFor="dealerPhone">
                Phone Number
              </label>
              <input
                id="dealerPhone"
                type="text"
                className="iid-input font-mono"
                placeholder="(555) 123-4567"
                value={phone}
                onChange={e => setPhone(e.target.value)}
                onBlur={() => setTouched(prev => ({ ...prev, phone: true }))}
              />
            </div>
          </div>

          {/* City & State Row */}
          <div className="form-row two-col">
            <div className="form-group">
              <label className="iid-form-label" htmlFor="dealerCity">
                City *
              </label>
              <input
                id="dealerCity"
                type="text"
                className="iid-input"
                placeholder="E.g., Los Angeles"
                value={city}
                onChange={e => setCity(e.target.value)}
                onBlur={() => setTouched(prev => ({ ...prev, city: true }))}
              />
              {touched.city && isCityInvalid && <span className="error-text">City is required.</span>}
            </div>

            <div className="form-group">
              <label className="iid-form-label" htmlFor="dealerState">
                State / Region *
              </label>
              <input
                id="dealerState"
                type="text"
                className="iid-input uppercase"
                placeholder="E.g., CA, TX, NY"
                value={state}
                onChange={e => setState(e.target.value)}
                onBlur={() => setTouched(prev => ({ ...prev, state: true }))}
              />
              {touched.state && isStateInvalid && (
                <span className="error-text">State code is required.</span>
              )}
            </div>
          </div>

          <div className="realtime-hint">
            <span className="material-symbols-outlined hint-icon">info</span>
            <span>
              Registered showrooms will immediately appear in the top-bar showroom selector, inventory
              filter, and SignalR live rooms.
            </span>
          </div>

          {/* Footer Buttons */}
          <div className="modal-footer">
            <button
              type="button"
              className="iid-btn iid-btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="iid-btn iid-btn-primary"
              disabled={isSubmitting || isFormInvalid}
            >
              {isSubmitting ? (
                <>
                  <span className="material-symbols-outlined spin-icon">progress_activity</span>
                  <span>Saving...</span>
                </>
              ) : (
                <>
                  <span className="material-symbols-outlined">
                    {isEditMode ? 'check_circle' : 'add_circle'}
                  </span>
                  <span>{isEditMode ? 'Save Changes' : 'Register Dealership'}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
