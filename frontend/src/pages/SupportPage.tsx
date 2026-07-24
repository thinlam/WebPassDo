import { useState } from 'react'
import { PageHeader, Section } from '../components/common/ui'

const FAQ_ITEMS = [
  {
    q: 'Làm sao để đăng bán sản phẩm?',
    a: 'Vào mục "Đăng bán" trên thanh menu, điền thông tin sản phẩm, chọn ảnh và bấm "Đăng sản phẩm". Bạn cần thêm địa chỉ lấy hàng và tài khoản ngân hàng trong phần Cài đặt trước khi đăng.',
  },
  {
    q: 'Quy trình mua hàng như thế nào?',
    a: 'Chọn sản phẩm → Bấm "Mua" → Chọn địa chỉ giao hàng → Chọn phương thức thanh toán → Xác nhận đặt hàng. Người bán sẽ xác nhận và chuẩn bị hàng.',
  },
  {
    q: 'Phí vận chuyển được tính như thế nào?',
    a: 'Phí vận chuyển được tính dựa trên khoảng cách giữa địa chỉ lấy hàng và địa chỉ giao hàng. Giao hàng nội thành miễn phí. Phí sẽ hiển thị khi bạn chọn địa chỉ ở bước thanh toán.',
  },
  {
    q: 'Tôi có thể hủy đơn hàng không?',
    a: 'Bạn có thể hủy đơn khi đơn hàng đang ở trạng thái "Chờ thanh toán" hoặc "Chờ xác nhận". Sau khi người bán xác nhận và chuẩn bị hàng, không thể hủy đơn.',
  },
  {
    q: 'Làm sao để theo dõi đơn hàng?',
    a: 'Vào mục "Đơn mua" để xem danh sách đơn hàng. Bấm vào đơn hàng để xem chi tiết trạng thái, thông tin vận chuyển và lịch sử.',
  },
  {
    q: 'Thanh toán chuyển khoản hoạt động thế nào?',
    a: 'Sau khi đặt hàng, bạn sẽ nhận được thông tin tài khoản ngân hàng của người bán. Chuyển khoản theo nội dung được cung cấp, sau đó gửi ảnh minh chứng. Người bán sẽ xác nhận thanh toán.',
  },
]

const POLICIES = [
  {
    title: 'Chính sách hoàn trả',
    content: 'Sản phẩm có thể được hoàn trả trong vòng 3 ngày nếu không đúng mô tả hoặc bị lỗi. Liên hệ người bán qua tin nhắn để thỏa thuận.',
  },
  {
    title: 'Chính sách bảo mật',
    content: 'Thông tin cá nhân của bạn được mã hóa và bảo mật. Chúng tôi không chia sẻ thông tin với bên thứ ba ngoại trừ khi cần thiết cho việc giao hàng.',
  },
  {
    title: 'Quy tắc cộng đồng',
    content: 'Không đăng bán hàng giả, hàng cấm. Mô tả sản phẩm phải trung thực. Vi phạm sẽ bị khóa tài khoản.',
  },
]

export function SupportPage() {
  const [openFaq, setOpenFaq] = useState<number | null>(null)

  return (
    <Section className="max-w-3xl">
      <PageHeader title="Trung tâm hỗ trợ" description="Câu hỏi thường gặp và hướng dẫn sử dụng." />

      <div className="space-y-8">
        <div className="space-y-3">
          <h2 className="font-display text-xl text-ink">Câu hỏi thường gặp</h2>
          {FAQ_ITEMS.map((item, i) => (
            <div key={i} className="rounded-2xl border border-line bg-white/80">
              <button
                className="flex w-full items-center justify-between p-4 text-left text-sm font-medium text-ink transition hover:text-forest"
                onClick={() => setOpenFaq(openFaq === i ? null : i)}
              >
                {item.q}
                <svg
                  className={`h-4 w-4 shrink-0 transition ${openFaq === i ? 'rotate-180' : ''}`}
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
                </svg>
              </button>
              {openFaq === i && (
                <div className="border-t border-line px-4 py-3 text-sm text-muted">
                  {item.a}
                </div>
              )}
            </div>
          ))}
        </div>

        <div className="space-y-3">
          <h2 className="font-display text-xl text-ink">Chính sách</h2>
          {POLICIES.map((p, i) => (
            <div key={i} className="rounded-2xl border border-line bg-white/80 p-4">
              <h3 className="mb-2 font-medium text-ink">{p.title}</h3>
              <p className="text-sm text-muted">{p.content}</p>
            </div>
          ))}
        </div>

        <div className="rounded-2xl border border-line bg-sand/50 p-6 text-center">
          <h3 className="mb-2 font-display text-lg text-ink">Cần hỗ trợ thêm?</h3>
          <p className="text-sm text-muted">
            Liên hệ qua email: <a href="mailto:support@passdo.vn" className="text-forest hover:underline">support@passdo.vn</a>
          </p>
        </div>
      </div>
    </Section>
  )
}
