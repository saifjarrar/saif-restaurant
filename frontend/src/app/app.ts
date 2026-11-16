import { LanguageService } from './i18n/language';
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FoodParticlesComponent } from './shared/food-particles/food-particles';

@Component({
  imports: [RouterOutlet, FoodParticlesComponent],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  readonly locale = inject(LanguageService);
}
