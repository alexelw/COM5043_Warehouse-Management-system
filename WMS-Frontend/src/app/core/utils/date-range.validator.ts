import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function dateRangeValidator(fromKey: string, toKey: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const fromValue = control.get(fromKey)?.value as string | null | undefined;
    const toValue = control.get(toKey)?.value as string | null | undefined;

    if (!fromValue || !toValue) {
      return null;
    }

    return fromValue <= toValue ? null : { dateRange: true };
  };
}

export function isIsoDate(value: string | null | undefined): boolean {
  if (!value) {
    return true;
  }

  return /^\d{4}-\d{2}-\d{2}$/.test(value);
}
