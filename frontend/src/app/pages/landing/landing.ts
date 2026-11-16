import { TranslatePipe } from '../../i18n/translate.pipe';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  imports: [TranslatePipe],
  selector: 'app-landing',
  styleUrl: './landing.css',
  templateUrl: './landing.html',
})
export class Landing {
  router = inject(Router);
  onLogin() {
    this.router.navigateByUrl('/login');
  }
}
