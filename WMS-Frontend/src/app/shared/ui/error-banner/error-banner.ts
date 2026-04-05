import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ApiErrorState } from '../../../core/models/api.types';

@Component({
  selector: 'app-error-banner',
  templateUrl: './error-banner.html',
  styleUrl: './error-banner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorBannerComponent {
  readonly error = input<ApiErrorState | null>(null);
  readonly title = input('Something went wrong');
}
