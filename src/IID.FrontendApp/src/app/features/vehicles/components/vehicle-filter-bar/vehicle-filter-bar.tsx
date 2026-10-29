import { KeyboardEvent, useEffect, useRef, useState } from 'react';
import { VehicleFilterParams, VehicleStatus } from '../../../../core/models/vehicle.model';
import './vehicle-filter-bar.scss';

export interface VehicleFilterBarProps {
  onFilterChange: (params: VehicleFilterParams) => void;
}

const TEXT_INPUT_DEBOUNCE_MS = 250;

type TextFieldKey = 'make' | 'model' | 'vin' | 'stock';

interface FilterSnapshot {
  searchMake: string;
  searchModel: string;
  searchVin: string;
  searchStock: string;
  selectedStatus: VehicleStatus | '';
  minAgeDays: number | null;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
}

export function VehicleFilterBarComponent({ onFilterChange }: VehicleFilterBarProps) {
  const [searchMake, setSearchMake] = useState<string>('');
  const [searchModel, setSearchModel] = useState<string>('');
  const [searchVin, setSearchVin] = useState<string>('');
  const [searchStock, setSearchStock] = useState<string>('');
  const [selectedStatus, setSelectedStatus] = useState<VehicleStatus | ''>('');
  const [minAgeDays, setMinAgeDays] = useState<number | null>(null);
  const [sortBy, setSortBy] = useState<string>('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const onFilterChangeRef = useRef(onFilterChange);
  onFilterChangeRef.current = onFilterChange;

  const snapshotRef = useRef<FilterSnapshot>({
    searchMake: '',
    searchModel: '',
    searchVin: '',
    searchStock: '',
    selectedStatus: '',
    minAgeDays: null,
    sortBy: 'createdAt',
    sortOrder: 'desc'
  });

  const timersRef = useRef<Partial<Record<TextFieldKey, ReturnType<typeof setTimeout>>>>({});
  const lastEmittedTextRef = useRef<Partial<Record<TextFieldKey, string>>>({});

  function clearAllTimers(): void {
    const timers = timersRef.current;
    (Object.keys(timers) as TextFieldKey[]).forEach(key => {
      if (timers[key] !== undefined) {
        clearTimeout(timers[key]);
        delete timers[key];
      }
    });
  }

  useEffect(() => {
    return () => {
      clearAllTimers();
    };
  }, []);

  function emitFilters(snap: FilterSnapshot = snapshotRef.current): void {
    const params: VehicleFilterParams = {
      make: snap.searchMake.trim() || undefined,
      model: snap.searchModel.trim() || undefined,
      vin: snap.searchVin.trim() || undefined,
      stockNumber: snap.searchStock.trim() || undefined,
      status: snap.selectedStatus ? (snap.selectedStatus as VehicleStatus) : undefined,
      minAgeDays: snap.minAgeDays !== null ? snap.minAgeDays : undefined,
      sort: snap.sortBy,
      order: snap.sortOrder
    };
    onFilterChangeRef.current(params);
  }

  function scheduleTextDebounce(key: TextFieldKey, value: string): void {
    if (timersRef.current[key] !== undefined) {
      clearTimeout(timersRef.current[key]);
    }
    timersRef.current[key] = setTimeout(() => {
      delete timersRef.current[key];
      if (lastEmittedTextRef.current[key] !== value) {
        lastEmittedTextRef.current[key] = value;
        emitFilters(snapshotRef.current);
      }
    }, TEXT_INPUT_DEBOUNCE_MS);
  }

  function onMakeInput(value: string): void {
    setSearchMake(value);
    snapshotRef.current.searchMake = value;
    scheduleTextDebounce('make', value);
  }

  function onModelInput(value: string): void {
    setSearchModel(value);
    snapshotRef.current.searchModel = value;
    scheduleTextDebounce('model', value);
  }

  function onVinInput(value: string): void {
    setSearchVin(value);
    snapshotRef.current.searchVin = value;
    scheduleTextDebounce('vin', value);
  }

  function onStockInput(value: string): void {
    setSearchStock(value);
    snapshotRef.current.searchStock = value;
    scheduleTextDebounce('stock', value);
  }

  function onTextKeyDown(e: KeyboardEvent<HTMLInputElement>): void {
    if (e.key === 'Enter') {
      clearAllTimers();
      lastEmittedTextRef.current = {
        make: snapshotRef.current.searchMake,
        model: snapshotRef.current.searchModel,
        vin: snapshotRef.current.searchVin,
        stock: snapshotRef.current.searchStock
      };
      emitFilters(snapshotRef.current);
    }
  }

  function onStatusChange(value: VehicleStatus | ''): void {
    setSelectedStatus(value);
    snapshotRef.current.selectedStatus = value;
    emitFilters(snapshotRef.current);
  }

  function onMinAgeChange(rawValue: string): void {
    const parsed = rawValue ? Number(rawValue) : null;
    setMinAgeDays(parsed);
    snapshotRef.current.minAgeDays = parsed;
    emitFilters(snapshotRef.current);
  }

  function onSortByChange(value: string): void {
    setSortBy(value);
    snapshotRef.current.sortBy = value;
    emitFilters(snapshotRef.current);
  }

  function toggleSortOrder(): void {
    const next: 'asc' | 'desc' = snapshotRef.current.sortOrder === 'asc' ? 'desc' : 'asc';
    setSortOrder(next);
    snapshotRef.current.sortOrder = next;
    emitFilters(snapshotRef.current);
  }

  function reset(): void {
    clearAllTimers();
    const defaults: FilterSnapshot = {
      searchMake: '',
      searchModel: '',
      searchVin: '',
      searchStock: '',
      selectedStatus: '',
      minAgeDays: null,
      sortBy: 'createdAt',
      sortOrder: 'desc'
    };
    snapshotRef.current = defaults;
    lastEmittedTextRef.current = {
      make: '',
      model: '',
      vin: '',
      stock: ''
    };
    setSearchMake('');
    setSearchModel('');
    setSearchVin('');
    setSearchStock('');
    setSelectedStatus('');
    setMinAgeDays(null);
    setSortBy('createdAt');
    setSortOrder('desc');
    emitFilters(defaults);
  }

  return (
    <div className="iid-vehicle-filter-bar filter-bar iid-card">
      {/* Row 1: Make, Model, VIN & Stock # */}
      <div className="filter-row filter-row-primary">
        {/* Search Make */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterMake">
            Make
          </label>
          <div className="input-with-icon">
            <span className="material-symbols-outlined input-icon">directions_car</span>
            <input
              id="filterMake"
              type="text"
              className="iid-input input-field"
              placeholder="e.g. Tesla, BMW"
              value={searchMake}
              onChange={e => onMakeInput(e.target.value)}
              onKeyDown={onTextKeyDown}
            />
          </div>
        </div>

        {/* Search Model */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterModel">
            Model
          </label>
          <div className="input-with-icon">
            <span className="material-symbols-outlined input-icon">search</span>
            <input
              id="filterModel"
              type="text"
              className="iid-input input-field"
              placeholder="e.g. Model 3, X5"
              value={searchModel}
              onChange={e => onModelInput(e.target.value)}
              onKeyDown={onTextKeyDown}
            />
          </div>
        </div>

        {/* Search VIN */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterVin">
            VIN
          </label>
          <div className="input-with-icon">
            <span className="material-symbols-outlined input-icon">fingerprint</span>
            <input
              id="filterVin"
              type="text"
              className="iid-input input-field"
              placeholder="e.g. 5YJ, 1HG..."
              value={searchVin}
              onChange={e => onVinInput(e.target.value)}
              onKeyDown={onTextKeyDown}
            />
          </div>
        </div>

        {/* Search Stock # */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterStock">
            Stock #
          </label>
          <div className="input-with-icon">
            <span className="material-symbols-outlined input-icon">tag</span>
            <input
              id="filterStock"
              type="text"
              className="iid-input input-field"
              placeholder="e.g. STK-ATX..."
              value={searchStock}
              onChange={e => onStockInput(e.target.value)}
              onKeyDown={onTextKeyDown}
            />
          </div>
        </div>
      </div>

      {/* Row 2: Secondary Filters */}
      <div className="filter-row filter-row-secondary">
        {/* Status Dropdown */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterStatus">
            Status
          </label>
          <select
            id="filterStatus"
            className="iid-input iid-select"
            value={selectedStatus}
            onChange={e => onStatusChange(e.target.value as VehicleStatus | '')}
          >
            <option value="">All Statuses</option>
            <option value="Available">Available</option>
            <option value="Pending">Pending</option>
            <option value="Sold">Sold</option>
            <option value="Wholesale">Wholesale</option>
          </select>
        </div>

        {/* Min Age Filter */}
        <div className="filter-item">
          <label className="iid-form-label" htmlFor="filterMinAge">
            Min Days On Lot
          </label>
          <select
            id="filterMinAge"
            className="iid-input iid-select"
            value={minAgeDays ?? ''}
            onChange={e => onMinAgeChange(e.target.value)}
          >
            <option value="">Any Duration</option>
            <option value="30">30+ Days (Warning)</option>
            <option value="60">60+ Days (High Aging)</option>
            <option value="90">90+ Days (Critical Aging)</option>
          </select>
        </div>

        {/* Sort By */}
        <div className="filter-item sort-item">
          <label className="iid-form-label" htmlFor="filterSort">
            Sort Order
          </label>
          <div className="sort-controls">
            <select
              id="filterSort"
              className="iid-input iid-select"
              value={sortBy}
              onChange={e => onSortByChange(e.target.value)}
            >
              <option value="createdAt">Date Created</option>
              <option value="soldAt">Date Sold</option>
              <option value="askingPrice">Asking Price</option>
              <option value="mileage">Mileage</option>
              <option value="year">Model Year</option>
            </select>
            <button
              type="button"
              className="sort-toggle-btn"
              onClick={toggleSortOrder}
              title={sortOrder === 'asc' ? 'Ascending' : 'Descending'}
            >
              <span className="material-symbols-outlined">
                {sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'}
              </span>
            </button>
          </div>
        </div>

        {/* Action Reset */}
        <div className="filter-actions">
          <button type="button" className="iid-btn iid-btn-secondary reset-btn" onClick={reset}>
            <span className="material-symbols-outlined">restart_alt</span>
            <span>Reset</span>
          </button>
        </div>
      </div>
    </div>
  );
}
