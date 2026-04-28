import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-ai-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chatbot.component.html',
  styleUrl: './ai-chatbot.component.css'
})
export class AiChatbotComponent {
  isOpen = signal(false);
  message = signal('');
  messages = signal<{role: 'user' | 'ai', text: string}[]>([
    { role: 'ai', text: 'Hello! I am your AI Store Assistant. How can I help you today?' }
  ]);

  constructor(private apiService: ApiService) {}

  toggleChat() {
    this.isOpen.set(!this.isOpen());
  }

  sendMessage() {
    if (!this.message().trim()) return;
    
    const userText = this.message();
    this.messages.update(m => [...m, { role: 'user', text: userText }]);
    this.message.set('');

    this.apiService.chatWithAI(userText).subscribe({
      next: (res: any) => {
        this.messages.update(m => [...m, { role: 'ai', text: res.response }]);
      },
      error: (err) => {
        this.messages.update(m => [...m, { 
          role: 'ai', 
          text: 'I am having trouble connecting to the backend. Please make sure the AIService is running!' 
        }]);
      }
    });
  }
}
